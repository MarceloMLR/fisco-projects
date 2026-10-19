using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Tests;

// Spec: clientes, requirements "Dados do cliente" and "Logo do cliente" (module side).
public sealed class ClientTests
{
    [Fact(DisplayName = "Nome com até 150 caracteres")]
    public void ValidName_AcceptsUpTo150Characters()
    {
        Client.ValidName("  Alfa Distribuidora ").Should().Be("Alfa Distribuidora");
        Client.ValidName(new string('a', 150)).Should().HaveLength(150);
        Client.ValidName(new string('a', 151)).Should().BeNull();
        Client.ValidName("  ").Should().BeNull();
    }

    [Fact(DisplayName = "Observação com até 500 caracteres")]
    public void Observation_HasUpTo500Characters()
    {
        Client.TryNormalizeObservation("  Matriz em Campinas ", out var observation).Should().BeTrue();
        observation.Should().Be("Matriz em Campinas");
        Client.TryNormalizeObservation(" ", out var empty).Should().BeTrue();
        empty.Should().BeNull();
        Client.TryNormalizeObservation(new string('a', 501), out _).Should().BeFalse();
    }

    [Fact(DisplayName = "Cliente começa ativo")]
    public void NewClient_IsActive_WithoutALogo()
    {
        var client = Client.Create("Alfa Distribuidora", "11222333000181", null);

        client.IsActive.Should().BeTrue();
        client.HasLogo.Should().BeFalse();
    }

    [Fact(DisplayName = "CNPJ fora da regra no domínio")]
    public void WrongCnpj_IsRefusedByTheClient()
    {
        var create = () => Client.Create("Alfa Distribuidora", "11222333000180", null);

        create.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Logo PNG e JPG")]
    public void PngAndJpg_AreAccepted()
    {
        ClientLogo.Check(Logos.Png).Format.Should().Be(ClientLogoFormat.Png);
        ClientLogo.Check(Logos.Jpg).Format.Should().Be(ClientLogoFormat.Jpg);
    }

    [Fact(DisplayName = "Logo do cliente recusado")]
    public void OtherFormats_AndLargeFiles_AreRefused()
    {
        var large = new byte[ClientLogo.MaxBytes + 1];
        Logos.Png.CopyTo(large, 0);

        ClientLogo.Check(Logos.Svg).Refusal.Should().Be(ClientLogoRefusal.Format);
        ClientLogo.Check([0x47, 0x49, 0x46, 0x38]).Refusal.Should().Be(ClientLogoRefusal.Format);
        ClientLogo.Check(large).Refusal.Should().Be(ClientLogoRefusal.TooLarge);
    }
}

/// <summary>Logos of the tests, made only of the first bytes of each format.</summary>
internal static class Logos
{
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    public static readonly byte[] Jpg = [0xFF, 0xD8, 0xFF, 0xE0, 4, 5, 6];
    public static readonly byte[] Svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"/>"u8.ToArray();
}
