namespace Oc.BinGrid.Domain;

public class GridBinDbProperties
{
    public static string DbTablePrefix { get; set; } = "";

    public static string DbSchema { get; set; } = null;

    public const string ConnectionStringName = "Default";
}
