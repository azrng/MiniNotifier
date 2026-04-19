using MiniNotifier.Models;

namespace MiniNotifier.Services.Interfaces;

public interface IMouseActivityService
{
    void Initialize();

    MouseActivitySnapshot GetSnapshot();
}
