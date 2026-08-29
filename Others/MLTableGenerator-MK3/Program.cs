using System.Windows.Forms;
using System.Drawing;
using Colorful;
using Console = Colorful.Console;
using VedAstro.Library;
using ShellProgressBar;
using ProgressBar = ShellProgressBar.ProgressBar;

namespace MLTableGenerator
{
    internal class Program
    {
        [STAThread]
        static async Task Main(string[] args)
        {
            //Console.WriteWithGradient("Hello, World!", Color.Red, Color.Blue, 14);
            //Console.WriteStyled("Hello, World!", new StyleSheet(Color.Yellow));
            //greet message
            //FigletFont font = FigletFont.Load(@"C:\Users\ASUS\Desktop\Projects\VedAstro\MLTableGenerator\banner.flf");
            //Figlet figlet = new Figlet(font);
            //Console.WriteLine(figlet.ToAscii("VedAstro"), Color.Magenta);
            Console.WriteAscii("ML Table Generator", Color.Yellow);
            Console.WriteLine("VedAstro - 2023\n\n", Color.Magenta);


            //# STEP 1
            //ask user to enter path to time source file
            Console.WriteLine("STEP 1:", Color.Yellow);
            Console.WriteLine("Press ENTER, to select Source EXCEL file with list of Time.");
            Console.ReadLine();

            //show GUI and to let user find file
            var dialog = new OpenFileDialog();
            var result = dialog.ShowDialog();
            var sourceFilePath = dialog.FileName;

            //let user know file selected
            Console.WriteLine($"Selected File:\n{sourceFilePath}");

            //# STEP 2
            Console.WriteLine("\nSTEP 2:", Color.Yellow);
            Console.WriteLine("Processing file...\n");

            int totalTicks = 2;
            var options = new ProgressBarOptions
            {
                ProgressCharacter = '─',
                ProgressBarOnBottom = true
            };
            using (var pbar = new ProgressBar(totalTicks, "initial message", options))
            {

                //get file as binary
                pbar.Tick($"Reading file... {1} of {totalTicks}");
                using var inputedFile = File.OpenRead(sourceFilePath);

                pbar.Tick($"Parsing Time and Location Columns... {2} of {totalTicks}");
                var returnList = await MLTable.GetTimeListFromExcel(inputedFile);


            }


            Console.ReadLine();
        }
    }
}
