using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

var configuration = new ConfigurationBuilder().AddCommandLine(args).Build();
var inputFileName = configuration["i"] ?? throw new Exception("input file not specified");
var inputAtlasFileName = configuration["a"] ?? throw new Exception("atlas file not specified");
var outputDirectoryPath = configuration["o"] ?? "sprites";
var zoom = float.TryParse(configuration["zoom"], out var z) ? z : 8f;

var lines = await File.ReadAllLinesAsync(inputFileName);
var atlas = await Image.LoadAsync(inputAtlasFileName);
Directory.CreateDirectory(outputDirectoryPath);

var backgroundRecolorBrush = new RecolorBrush(Color.Cyan, Color.Transparent, 0);
var clearBackgroundOptions = new DrawingOptions
{
    GraphicsOptions = { AlphaCompositionMode = PixelAlphaCompositionMode.Src }
};

var data = lines
    .Select((x, i) => (x, i))
    .Where(t => t.x.StartsWith("struct"))
    .Select(
        t =>
        {
            static int ExtractInt(string value) => int.Parse(Regex.Match(value, @"\d+").Value);

            var id = ExtractInt(t.x);
            var uvList = lines.Skip(t.i + 1).Take(4).Select(ExtractInt).ToList();
            var uv = new Rectangle(uvList[0], uvList[1], uvList[2], uvList[3]);
            return (id, uv);
        }
    )
    .Where(t => !t.uv.Size.IsEmpty)
    .Select(
        t => (t.id,
            sprite: atlas.Clone(
                x => x.Crop(t.uv)
                    .Fill(clearBackgroundOptions, backgroundRecolorBrush)
                    .Resize((Size)(t.uv.Size * zoom), KnownResamplers.NearestNeighbor, true)
            ))
    );

await Task.WhenAll(data.Select(t => t.sprite.SaveAsPngAsync(Path.Combine(outputDirectoryPath, $"{t.id:X}.png"))));

