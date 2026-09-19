using System.Threading.Tasks;

namespace HeyClicky.Vision
{
    public interface IVisionProvider
    {
        Task<ScreenAnalysis> AnalyzeScreenAsync(byte[] screenshot, string instruction);
    }
}
