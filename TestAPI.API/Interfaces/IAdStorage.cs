using TestAPI.Models;

namespace TestAPI.Interfaces
{
    public interface IAdStorage
    {
        Task LoadFromFileAsync(string path);
        List<string> FindPlatforms(string location);
        Dictionary<string, List<string>> GetAllPlatforms();
        int GetDeclaredLocationCount();
    }
}
