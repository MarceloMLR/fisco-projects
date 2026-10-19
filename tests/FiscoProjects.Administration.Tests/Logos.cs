using System.Text;

namespace FiscoProjects.Administration.Tests;

/// <summary>Contents of logos for the tests: a PNG by its signature, SVGs written here, a JPG by its first bytes.</summary>
internal static class Logos
{
    public static byte[] Png(int size = 64)
    {
        var content = new byte[size];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(content, 0);
        return content;
    }

    public static byte[] Jpg() => [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];

    public static byte[] Svg(string inside = "<rect width=\"10\" height=\"10\" fill=\"#1f5fbf\"/>") =>
        Encoding.UTF8.GetBytes($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 10 10\">{inside}</svg>");
}
