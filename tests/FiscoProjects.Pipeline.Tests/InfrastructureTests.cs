using System.Text.RegularExpressions;
using FiscoProjects.Testing;

namespace FiscoProjects.Pipeline.Tests;

// Spec: infraestrutura (ADR-0053, ADR-0114). The Terraform code is read as text; the CI also formats and validates it
// with Terraform itself (scripts/ci/infra.sh).
public sealed partial class InfrastructureTests
{
    private static readonly string Folder = RepositoryFiles.PathOf("infra", "terraform");

    private static readonly string[] Modules = ["alerts", "app", "ci", "database", "entra", "key-vault", "monitoring"];

    [Fact(DisplayName = "Ambiente criado pelo Terraform")]
    public void Terraform_DescribesTheWholeInstallation_WithPinnedVersions()
    {
        var main = Read("main.tf");

        Directory.GetDirectories(Path.Combine(Folder, "modules")).Select(Path.GetFileName).Order().Should().Equal(Modules);
        foreach (var module in Modules)
        {
            main.Should().Contain($"source = \"./modules/{module}\"");
        }

        var versions = Read("versions.tf");
        var terraform = RequiredTerraform().Match(versions).Groups["version"].Value;
        terraform.Should().NotBeEmpty();
        Repository.Read("scripts", "ci", "terraform.sh").Should().Contain($"version={terraform}\n", "the CI checks with the same Terraform");
        ProviderVersions().Matches(versions).Should().HaveCount(2).And.AllSatisfy(match => match.Groups["version"].Value.Should().MatchRegex(@"^\d+\.\d+\.\d+$"));
        File.Exists(Path.Combine(Folder, ".terraform.lock.hcl")).Should().BeTrue("the providers are checked by their hashes");
        Read("environments", "dev.tfvars").Should().Contain("installation = \"").And.Contain("environment  = \"dev\"");
    }

    [Fact(DisplayName = "Banco sem senha")]
    public void Database_HasNoPassword_AndTheApplicationUsesItsIdentity()
    {
        var code = AllCode();
        var app = Read("modules", "app", "main.tf");

        code.Should().NotContain("administrator_login_password");
        Read("modules", "database", "main.tf").Should().Contain("azuread_authentication_only = true");
        app.Should().Contain("Authentication=Active Directory Managed Identity").And.NotContain("Password=");
        app.Should().Contain("type = \"SystemAssigned\"").And.Contain("role_definition_name = \"Key Vault Secrets User\"");
    }

    [Fact(DisplayName = "Segredos fora do Terraform")]
    public void Terraform_CreatesNoSecret_AndKeepsPersonalValuesOutOfTheRepository()
    {
        var code = AllCode();

        foreach (var resource in new[] { "azurerm_key_vault_secret", "azurerm_key_vault_certificate", "azurerm_key_vault_key", "random_password", "azuread_application_password", "azuread_service_principal_password" })
        {
            code.Should().NotContain($"resource \"{resource}\"");
        }

        Read("modules", "app", "main.tf").Should().Contain("@Microsoft.KeyVault(VaultName=${var.key_vault_name};SecretName=Syncfusion--LicenseKey)");
        var ignored = Repository.Read(".gitignore").Split('\n');
        ignored.Should().Contain(".terraform/").And.Contain("*.tfstate").And.Contain("*.tfstate.*").And.Contain("*.auto.tfvars");
        foreach (var file in Directory.GetFiles(Path.Combine(Folder, "environments")))
        {
            var lines = File.ReadAllLines(file);
            lines.Should().NotContain(line => PersonalValue().IsMatch(line), "the operators and the e-mail of the alerts are personal");
            lines.Should().NotContain(line => EmailAddress().IsMatch(line));
        }
    }

    [Fact(DisplayName = "Backups da produção")]
    public void Defaults_AreThoseOfProduction()
    {
        var variables = Read("variables.tf");

        Default(variables, "database_sku").Should().Be("\"S0\"");
        Default(variables, "database_restore_days").Should().Be("14");
        Default(variables, "database_monthly_backup_months").Should().Be("12");
        Default(variables, "database_backup_redundancy").Should().Be("\"Zone\"");
        Default(variables, "aspnetcore_environment").Should().Be("\"Production\"");
        Default(variables, "app_is_database_admin").Should().Be("false");
        Default(variables, "location").Should().Be("\"brazilsouth\"");
        Default(variables, "app_location").Should().Be("null", "the application and the database stay in the region of the installation");
        Read("modules", "key-vault", "main.tf").Should().Contain("purge_protection_enabled   = true");
        Read("modules", "app", "main.tf").Should().Contain("https_only                                     = true");
    }

    [Fact(DisplayName = "Ambiente de desenvolvimento apagado sem custos")]
    public void DevelopmentEnvironment_KeepsNothingBilledAfterDestroy()
    {
        var dev = Read("environments", "dev.tfvars");

        dev.Should().Contain("database_monthly_backup_months = 0").And.Contain("database_sku                   = \"Basic\"");
        Read("modules", "database", "main.tf").Should().Contain("for_each = var.monthly_backup_months > 0 ? [var.monthly_backup_months] : []");
    }

    [Fact(DisplayName = "Alertas da instalação")]
    public void Alerts_CoverDownErrorsQueueAndDatabase()
    {
        var alerts = Read("modules", "alerts", "main.tf");

        alerts.Should().Contain("resource \"azurerm_application_insights_standard_web_test\" \"health\"")
            .And.Contain("application_insights_web_test_location_availability_criteria")
            .And.Contain("metric_name      = \"requests/failed\"")
            .And.Contain("Name == \"fiscoprojects.emails.oldest_waiting\"")
            .And.Contain("queue_stopped_after = 900")
            .And.Contain("metric_name      = \"storage_percent\"")
            .And.Contain("threshold        = 80")
            .And.Contain("email_address           = var.alert_email");
        Read("main.tf").Should().Contain("health_url                = \"https://${local.host}/health\"");
    }

    [Fact(DisplayName = "Estado do Terraform")]
    public void State_IsKeptInAProtectedStorageAccount()
    {
        var script = Repository.Read("infra", "scripts", "Prepare-TerraformState.ps1");

        Read("versions.tf").Should().Contain("backend \"azurerm\" {\n    use_azuread_auth = true\n  }");
        script.Should().Contain("--allow-shared-key-access false")
            .And.Contain("--allow-blob-public-access false")
            .And.Contain("--enable-versioning true")
            .And.Contain("--enable-delete-retention true")
            .And.Contain("--enable-container-delete-retention true")
            .And.Contain("$ErrorActionPreference = 'Stop'");
        Read("environments", "dev.backend.hcl").Should().Contain("container_name       = \"tfstate\"");
    }

    [Fact(DisplayName = "Identidade do pipeline com poder limitado")]
    public void PipelineIdentity_PublishesAndReads_WithoutGivingRolesOrChangingEntra()
    {
        var ci = Read("modules", "ci", "main.tf");

        ci.Should().Contain("issuer         = \"https://token.actions.githubusercontent.com\"")
            .And.Contain("subject        = \"repo:${var.repository}:environment:${var.environment}\"")
            .And.Contain("role_definition_name = \"Contributor\"\n  principal_id")
            .And.Contain("role_definition_name = \"Storage Blob Data Contributor\"")
            .And.Contain("scope                = var.state_account_id\n  role_definition_name = \"Reader\"")
            .And.Contain("app_role_ids[\"Directory.Read.All\"]")
            .And.Contain("app_role_ids[\"Application.Read.All\"]");
        ci.Should().NotContain("Owner").And.NotContain("User Access Administrator").And.NotContain("ReadWrite")
            .And.NotContain("azuread_application_password");
        Regex.Matches(ci, "resource \"azurerm_role_assignment\"").Should().HaveCount(3);
        Regex.Matches(ci, "resource \"azuread_app_role_assignment\"").Should().HaveCount(2);
        Read("main.tf").Should().Contain("resource_group_id  = azurerm_resource_group.main.id");
        // The container of the state has the names of the script that creates it.
        var script = Repository.Read("infra", "scripts", "Prepare-TerraformState.ps1");
        script.Should().Contain("$group = \"rg-fiscoprojects-$Installation-tfstate\"").And.Contain("$account = \"stfp${Installation}tfstate\"");
        Read("main.tf").Should().Contain("resourceGroups/rg-fiscoprojects-${var.installation}-tfstate/providers/Microsoft.Storage/storageAccounts/stfp${var.installation}tfstate\"\n")
            .And.Contain("state_container_id = \"${local.state_account_id}/blobServices/default/containers/tfstate\"");
        Regex.Match(Read("variables.tf"), "variable \"alert_email\" \\{[^}]*sensitive   = true", RegexOptions.Singleline).Success
            .Should().BeTrue("the plans are shown in public logs");
    }

    [Fact(DisplayName = "Aplicativo do Entra para a entrada")]
    public void EntraApplication_IsReadyForTheSignIn()
    {
        var entra = Read("modules", "entra", "main.tf");
        var main = Read("main.tf");

        entra.Should().Contain("redirect_uris = [var.redirect_uri]")
            .And.Contain("id_token_issuance_enabled     = false")
            .And.Contain("name = \"acct\"").And.Contain("name = \"email\"")
            .And.Contain("sign_in_scopes = [\"openid\", \"profile\", \"email\"]")
            .And.Contain("claim_values                         = local.sign_in_scopes")
            .And.Contain("app_role_assignment_required = var.assignment_required")
            .And.Contain("feature_tags {\n    enterprise = true\n  }");
        Regex.Matches(entra, "required_resource_access \\{").Should().HaveCount(1, "only the scopes of the sign-in, for now");
        main.Should().Contain("redirect_uri              = \"https://${local.host}/signin-oidc\"")
            .And.Contain("app_identity_principal_id = module.app.principal_id")
            .And.Contain("EntraSignIn__TenantId = data.azurerm_client_config.current.tenant_id")
            .And.Contain("EntraSignIn__ClientId = module.entra.client_id")
            .And.Contain("app_settings           = merge(var.app_settings, local.entra_settings)");
        Read("modules", "app", "main.tf").Should().Contain("ASPNETCORE_FORWARDEDHEADERS_ENABLED = \"true\"");
        var variables = Read("variables.tf");
        Default(variables, "entra_sign_in").Should().Be("true");
        Default(variables, "entra_assignment_required").Should().Be("true", "only the people assigned sign in (ADR-0005)");
        Default(variables, "first_administrator_object_id").Should().Be("null");
    }

    [Fact(DisplayName = "Aplicativo do Entra sem segredo")]
    public void EntraApplication_TrustsOnlyTheManagedIdentity()
    {
        var entra = Read("modules", "entra", "main.tf");

        Regex.Matches(entra, "resource \"azuread_application_federated_identity_credential\"").Should().HaveCount(1);
        entra.Should().Contain("audiences      = [\"api://AzureADTokenExchange\"]")
            .And.Contain("issuer         = \"https://login.microsoftonline.com/${var.tenant_id}/v2.0\"")
            .And.Contain("subject        = var.app_identity_principal_id")
            .And.NotContain("azuread_application_password").And.NotContain("azuread_application_certificate")
            .And.NotContain("azuread_service_principal_password").And.NotContain("azuread_service_principal_certificate");
    }

    [Fact(DisplayName = "Federação pelos números do repositório")]
    public void PipelineIdentity_TrustsTheImmutableSubjectOfGitHub()
    {
        // GitHub names the repository by the ids of the owner and of the repository in the subject of its tokens, so
        // that a renamed or recreated repository with the same name does not get the identity (ADR-0116).
        var validation = Regex.Match(Read("variables.tf"), "variable \"github_repository\" \\{.*?\\n\\}", RegexOptions.Singleline).Value;
        validation.Should().Contain("can(regex(\"^[^@/]+@[0-9]+/[^@/]+@[0-9]+$\", var.github_repository))");
        Read("environments", "dev.tfvars").Should().Contain("github_repository = \"MarceloMLR@106113485/fisco-projects@1423675463\"");
        Read("modules", "ci", "main.tf").Should().Contain("subject        = \"repo:${var.repository}:environment:${var.environment}\"");
    }

    private static string Read(params string[] relativePath) => File.ReadAllText(Path.Combine([Folder, .. relativePath]));

    private static string AllCode() =>
        string.Join('\n', Directory.GetFiles(Folder, "*.tf", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}.terraform{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));

    /// <summary>The default of a variable, as written.</summary>
    private static string Default(string variables, string name)
    {
        var block = Regex.Match(variables, $"variable \"{name}\" \\{{(?<body>.*?)\\n\\}}", RegexOptions.Singleline);
        block.Success.Should().BeTrue($"the variable {name} exists");
        return Regex.Match(block.Groups["body"].Value, @"\n\s*default\s*=\s*(?<value>[^\n]+)").Groups["value"].Value.Trim();
    }

    [GeneratedRegex("required_version = \"~> (?<version>[0-9.]+)\"")]
    private static partial Regex RequiredTerraform();

    [GeneratedRegex("version = \"= (?<version>[^\"]+)\"")]
    private static partial Regex ProviderVersions();

    [GeneratedRegex(@"^\s*(alert_email|operator_object_ids|first_administrator_object_id)\s*=")]
    private static partial Regex PersonalValue();

    [GeneratedRegex(@"[\w.+-]+@[\w-]+(\.[\w-]+)*\.[a-z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex EmailAddress();
}
