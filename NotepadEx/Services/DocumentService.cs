using System.IO;
using System.Threading.Tasks;
using NotepadEx.MVVM.Models;
using NotepadEx.Services.Interfaces;
using System.Drawing.Printing;

namespace NotepadEx.Services
{
    public class DocumentService : IDocumentService
    {
        // FIX: Implement the new interface method
        public async Task<string> LoadDocumentContentAsync(string filePath)
        {
            if(!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            return await File.ReadAllTextAsync(filePath);
        }

        // The old LoadDocumentAsync is now replaced by the one above.

        public async Task SaveDocumentAsync(Document document)
        {
            await File.WriteAllTextAsync(document.FilePath, document.Content);
            document.IsModified = false;
        }

        public void PrintDocument(Document document)
        {
            // ... (this method is unchanged)
            var printDialog = new System.Windows.Forms.PrintDialog();
            if(printDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var printDoc = new PrintDocument();
                printDoc.PrintPage += (sender, e) =>
                {
                    e.Graphics.DrawString(document.Content,
                        new System.Drawing.Font("Arial", 12),
                        System.Drawing.Brushes.Black,
                        new System.Drawing.RectangleF(100, 100, 700, 1000));
                };
                printDoc.Print();
            }
        }
    }
}