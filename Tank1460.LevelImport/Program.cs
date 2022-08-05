// See https://aka.ms/new-console-template for more information

using Tank1460.LevelImport;
using Tank1460.LevelImport.Properties;

var importer = new PngLevelImporter();

// либо можно сразу грузить как массив байтов, не перегоняя в Bitmap, это ещё и гораздо эффективнее, но надо не запарить с форматом
// https://stackoverflow.com/questions/19586524/get-all-pixel-information-of-an-image-efficiently
var image = importer.CreateBitmap(Resources.Battle_City__J__6);
var lvl = importer.ConvertPngToLvl(image);

Console.WriteLine(lvl);

// TODO: save to file {lvlnumber}.lvl

// TODO: loop for all png files in resources

Console.Write("Press Enter to continue...");
Console.ReadLine();