using UIKit;

namespace SSHAssistantFor1Password.GUI.Behaviors;

public partial class PlainEntryBehavior : PlatformBehavior<Entry, UITextField>
{
    protected override void OnAttachedTo(Entry bindable, UITextField platformView)
    {
        base.OnAttachedTo(bindable, platformView);

        platformView.BorderStyle = UITextBorderStyle.None;
    }
}
