using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Tests;

// Spec: clientes, requirement "CNPJ do cliente" (module side).
public sealed class CnpjTests
{
    [Theory(DisplayName = "CNPJ com e sem pontuação")]
    [InlineData("11.222.333/0001-81", "11222333000181")]
    [InlineData("11222333000181", "11222333000181")]
    [InlineData(" 11 222 333 0001 81 ", "11222333000181")]
    [InlineData("77.555.888/0001-53", "77555888000153")]
    public void NumericCnpj_IsKeptWithoutPunctuation(string text, string expected)
    {
        Cnpj.TryNormalize(text, out var cnpj).Should().BeTrue();
        cnpj.Should().Be(expected);
    }

    [Theory(DisplayName = "CNPJ alfanumérico")]
    [InlineData("12.ABC.345/01DE-35")]
    [InlineData("12.abc.345/01de-35")]
    [InlineData("12ABC34501DE35")]
    public void AlphanumericCnpj_IsAccepted(string text)
    {
        Cnpj.TryNormalize(text, out var cnpj).Should().BeTrue();
        cnpj.Should().Be("12ABC34501DE35");
    }

    [Theory(DisplayName = "CNPJ inválido")]
    [InlineData("11.222.333/0001-80")]
    [InlineData("11.222.333/0001-1")]
    [InlineData("11.111.111/1111-11")]
    [InlineData("00000000000000")]
    [InlineData("12.ABC.345/01DE-36")]
    [InlineData("12.ABC.345/01DE-3A")]
    [InlineData("1234567890123456")]
    [InlineData("CNPJ")]
    public void WrongCnpj_IsRefused(string text) =>
        Cnpj.TryNormalize(text, out _).Should().BeFalse();

    [Theory(DisplayName = "Sem CNPJ")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyCnpj_IsNone(string? text)
    {
        Cnpj.TryNormalize(text, out var cnpj).Should().BeTrue();
        cnpj.Should().BeNull();
    }

    [Theory(DisplayName = "CNPJ formatado")]
    [InlineData("11222333000181", "11.222.333/0001-81")]
    [InlineData("12ABC34501DE35", "12.ABC.345/01DE-35")]
    public void Cnpj_IsFormatted(string cnpj, string expected) =>
        Cnpj.Format(cnpj).Should().Be(expected);
}
