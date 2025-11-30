if (args.Length < 2)
{
    Console.WriteLine("Usage: CbaStatementToCsv <input.txt> <output.csv> [<year>]");
    return;
}

string inputFile = args[0];
string outputFile = args[1];
string year = args.Length >= 3 ? args[2] : DateTime.Now.Year.ToString();

// TODO: Add file conversion logic here
Console.WriteLine($"Input file: {inputFile}");
Console.WriteLine($"Output file: {outputFile}");

using var reader = new StreamReader(inputFile);
using var writer = new StreamWriter(outputFile);

writer.WriteLine("Date,Amount,Description");

int totalProcessedRows = 0;
int totalFailedLines = 0;
string? line;
for (int lineIndex = 0; (line = reader.ReadLine()) != null; lineIndex++)
{
    line = line.Trim();

    if (line == "") continue;

    // Find the second space
    int firstSpaceIndex = line.IndexOf(' ');
    if (firstSpaceIndex == -1) continue; // No spaces found
    int secondSpaceIndex = line.IndexOf(' ', firstSpaceIndex + 1);
    if (secondSpaceIndex == -1) continue; // Only one space found

    // Find the last space
    int lastSpaceIndex = line.LastIndexOf(' ');
    if (lastSpaceIndex == -1 || lastSpaceIndex == secondSpaceIndex) continue; // No last space found or only two spaces

    // Extract parts
    string dateStr = line[..secondSpaceIndex].Trim();
    string descriptionStr = line[secondSpaceIndex..lastSpaceIndex].Trim();
    string amountStr = line[lastSpaceIndex..].Trim();

    // if amountStr is tailed with a minus sign, move it to the front
    if (amountStr.EndsWith("-"))
    {
        amountStr = "-" + amountStr[..^1].Trim();
    }

    // parse the amountStr into decimal, note the amountStr may include commas as thousands separator
    if (!decimal.TryParse(amountStr, System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out decimal amount))
    {
        Console.WriteLine($"Line {lineIndex+1}: Failed to parse amount: {amountStr}");
        totalFailedLines++;
        continue;
    }

    if (DateTime.TryParseExact(dateStr + " " + year, "dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime date))
    {
        string formattedDate = date.ToString("dd/MM/yyyy");
        // The amount needs to be negated.
        writer.WriteLine($"{formattedDate},{-amount},\"{descriptionStr}\"");
        totalProcessedRows++;
    }
    else
    {
        Console.WriteLine($"Line {lineIndex+1}: Failed to parse date: {dateStr} (ref: {dateStr + " " + year})");
        totalFailedLines++;
    }
}
Console.WriteLine($"Total rows imported: {totalProcessedRows}");
Console.WriteLine($"Total lines failed: {totalFailedLines}");
