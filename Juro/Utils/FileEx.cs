using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Juro.Utils;

public class FileEx
{
    public static async Task CombineMultipleFilesIntoSingleFile(
        string inputDirectoryPath,
        string inputFileNamePattern,
        string outputFilePath)
    {
        var inputFilePaths = Directory.GetFiles(inputDirectoryPath, inputFileNamePattern);
        await CombineMultipleFilesIntoSingleFile(inputFilePaths, outputFilePath);
    }

    public static async Task CombineMultipleFilesIntoSingleFile(
        IEnumerable<string> inputFilePaths,
        string outputFilePath)
    {
        Debug.WriteLine($"Number of files: {inputFilePaths.Count()}.");
        using var outputStream = File.Create(outputFilePath);
        foreach (var inputFilePath in inputFilePaths)
        {
            using var inputStream = File.OpenRead(inputFilePath);
            // Buffer size can be passed as the second argument.
            await inputStream.CopyToAsync(outputStream);
            Debug.WriteLine($"The file {inputFilePath} has been processed.");
        }
    }
}