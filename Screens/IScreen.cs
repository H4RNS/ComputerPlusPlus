using GorillaNetworking;

namespace ComputerPlusPlus.Screens
{
    public interface IScreen
    {
        public string Title { get; }
        public string Description { get; }

        public string GetContent();
        public void OnKeyPressed(GorillaKeyboardButton button);
        public void Start();
    }
}
