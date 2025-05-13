namespace OPSSHAssistant.GUI.Behaviors;

public partial class PlainEntryBehavior : PlatformBehavior<Entry, TextBox>
{
    protected override void OnAttachedTo(Entry bindable, TextBox platformView)
    {
        base.OnAttachedTo(bindable, platformView);
        
        //platformView.BorderStyle = UITextBorderStyle.None;
    }
}