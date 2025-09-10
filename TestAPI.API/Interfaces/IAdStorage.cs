using TestAPI.Models;

namespace TestAPI.Interfaces
{
    public interface IAdStorage
    {
	    List<AdModel> Platforms { get; }
        Task LoadFromFileAsync(string filePath);
        List<string> FindPlatforms(string location);
    }
}
