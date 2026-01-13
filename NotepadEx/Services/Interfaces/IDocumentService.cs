using NotepadEx.MVVM.Models;
using System.Threading.Tasks;

namespace NotepadEx.Services.Interfaces
{
    public interface IDocumentService
    {
        Task<string> LoadDocumentContentAsync(string filePath);

        Task SaveDocumentAsync(Document document);
        void PrintDocument(Document document);
    }
}