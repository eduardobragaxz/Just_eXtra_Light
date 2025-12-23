namespace JustExtraLight.Commands;

public sealed partial class AddFolderCommand(MainPageViewModel mainPageViewModel) : CommandBase
{
    public async override void Execute(object? parameter)
    {
        await mainPageViewModel.AddFolderImages();
    }
}