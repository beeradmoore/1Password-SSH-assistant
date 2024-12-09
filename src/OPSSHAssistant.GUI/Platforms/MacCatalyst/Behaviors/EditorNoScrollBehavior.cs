using Microsoft.Maui.Platform;
using UIKit;

namespace OPSSHAssistant.GUI.Behaviors;

public partial class EditorNoScrollBehavior : PlatformBehavior<Editor, Microsoft.Maui.Platform.MauiTextView>
{
    protected override void OnAttachedTo(Editor bindable, Microsoft.Maui.Platform.MauiTextView platformView)
    {
        base.OnAttachedTo(bindable, platformView);
        
        platformView.ScrollEnabled = false;
    }
}