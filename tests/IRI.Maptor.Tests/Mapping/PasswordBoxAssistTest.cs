using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using IRI.Maptor.Presentation.Wpf.Behaviors;
using IRI.Maptor.Presentation.Wpf.Controls.Dialogs;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Presentation.Wpf.ViewModels.Dialogs;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The auth screens' password box (IRI.Maptor.Styles.Security.PasswordBox + PasswordBoxAssist): the
/// show-password toggle keeps the PasswordBox as the only source of the password, the plain-text copy is
/// emptied when hidden and cannot be copied out; plus the generated-password dialog and its clipboard copy.
/// </summary>
[Collection(WpfCollection.Name)]
public class PasswordBoxAssistTest
{
    private static void LoadAppResources()
    {
        var merged = Application.Current.Resources.MergedDictionaries;

        if (merged.Any(d => d.Source?.OriginalString.EndsWith("Maptor.All.xaml") == true))
            return;

        foreach (var source in new[]
                 {
                     "pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml",
                     "pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml",
                     "pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Green.xaml",
                     "pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/Maptor.All.xaml",
                 })
        {
            merged.Add(new ResourceDictionary { Source = new Uri(source) });
        }
    }

    // runs everything the behaviour queued on the dispatcher (template, focus, ...)
    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

    private static (Window Window, PasswordBox Box) Show()
    {
        LoadAppResources();

        var styles = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/Styles/Controls.SecurityInputs.xaml")
        };

        var box = new PasswordBox { Style = (Style)styles["IRI.Maptor.Styles.Security.PasswordBox"], Width = 260 };

        var window = new Window
        {
            Content = box,
            Width = 300,
            Height = 120,
            Left = -10000,
            Top = -10000,
            ShowInTaskbar = false,
            ShowActivated = false,
            WindowStyle = WindowStyle.None,
        };

        window.Show();
        Pump();

        return (window, box);
    }

    private static T Part<T>(PasswordBox box, string name) where T : FrameworkElement =>
        Assert.IsAssignableFrom<T>(box.Template.FindName(name, box));

    [Fact]
    public void Template_HasTheParts_AndIsOn()
    {
        WpfTestHost.Run(() =>
        {
            var (window, box) = Show();

            Assert.True(PasswordBoxAssist.GetIsEnabled(box));
            Assert.Equal(Visibility.Collapsed, Part<TextBox>(box, "PART_RevealedText").Visibility);
            Assert.NotNull(Part<System.Windows.Controls.Primitives.ToggleButton>(box, "PART_RevealToggle"));
            Assert.Equal(Visibility.Collapsed, Part<StackPanel>(box, "PART_CapsLockWarning").Visibility);

            window.Close();
        });
    }

    [Fact]
    public void Reveal_ShowsThePassword_AndEditsGoBack()
    {
        WpfTestHost.Run(() =>
        {
            var (window, box) = Show();
            var revealed = Part<TextBox>(box, "PART_RevealedText");

            box.Password = "s3cret";
            PasswordBoxAssist.SetIsRevealed(box, true);
            Pump();

            Assert.Equal(Visibility.Visible, revealed.Visibility);
            Assert.Equal("s3cret", revealed.Text);

            revealed.Text = "s3cret!";
            Assert.Equal("s3cret!", box.Password);

            // cleared by the view (a failed sign-in): the revealed text follows
            box.Clear();
            Assert.Equal(string.Empty, revealed.Text);

            window.Close();
        });
    }

    [Fact]
    public void Hide_EmptiesThePlainText_ButKeepsThePassword()
    {
        WpfTestHost.Run(() =>
        {
            var (window, box) = Show();
            var revealed = Part<TextBox>(box, "PART_RevealedText");

            box.Password = "abc123";
            PasswordBoxAssist.SetIsRevealed(box, true);
            Pump();

            PasswordBoxAssist.SetIsRevealed(box, false);
            Pump();

            Assert.Equal(string.Empty, revealed.Text);
            Assert.Equal(Visibility.Collapsed, revealed.Visibility);
            Assert.Equal("abc123", box.Password);

            window.Close();
        });
    }

    [Fact]
    public void Unload_HidesThePassword()
    {
        WpfTestHost.Run(() =>
        {
            var (window, box) = Show();

            box.Password = "abc";
            PasswordBoxAssist.SetIsRevealed(box, true);
            Pump();

            window.Content = null;
            Pump();

            Assert.False(PasswordBoxAssist.GetIsRevealed(box));

            window.Close();
        });
    }

    [Fact]
    public void RevealedText_CannotBeCopied()
    {
        WpfTestHost.Run(() =>
        {
            var (window, box) = Show();
            var revealed = Part<TextBox>(box, "PART_RevealedText");

            var before = Clipboard.ContainsText() ? Clipboard.GetText() : null;

            try
            {
                Clipboard.SetText("sentinel");

                box.Password = "do-not-copy";
                PasswordBoxAssist.SetIsRevealed(box, true);
                Pump();

                revealed.SelectAll();
                ApplicationCommands.Copy.Execute(null, revealed);
                ApplicationCommands.Cut.Execute(null, revealed);

                Assert.Equal("sentinel", Clipboard.GetText());
                Assert.Equal("do-not-copy", box.Password);
            }
            finally
            {
                if (before is null) Clipboard.Clear(); else Clipboard.SetText(before);

                window.Close();
            }
        });
    }

    [Fact]
    public void CapsLockWarning_ShowsWhenFlagged()
    {
        WpfTestHost.Run(() =>
        {
            var (window, box) = Show();

            PasswordBoxAssist.SetIsCapsLockWarningVisible(box, true);
            Pump();

            Assert.Equal(Visibility.Visible, Part<StackPanel>(box, "PART_CapsLockWarning").Visibility);

            window.Close();
        });
    }

    [Fact]
    public void GeneratedPasswordDialog_ShowsUserAndPassword()
    {
        WpfTestHost.Run(() =>
        {
            LoadAppResources();

            var viewModel = new GeneratedPasswordDialogViewModel("user@example.com", "Ab1-xY9z");

            var dialog = new GeneratedPasswordDialogView { DataContext = viewModel };

            // bindings of a window that is not shown resolve on the dispatcher
            Pump();

            var password = Assert.IsType<TextBox>(dialog.FindName("passwordText"));

            Assert.Equal("Ab1-xY9z", password.Text);
            Assert.True(password.IsReadOnly);
            Assert.Contains("user@example.com", viewModel.Message);
            Assert.False(viewModel.IsCopied);

            dialog.Close();
        });
    }

    [Fact]
    public void CopySensitiveText_CopiesThenClears_UnlessReplaced()
    {
        WpfTestHost.Run(() =>
        {
            var before = Clipboard.ContainsText() ? Clipboard.GetText() : null;

            try
            {
                Assert.True(ClipboardHelper.CopySensitiveText("Temp-Pass1", TimeSpan.FromMilliseconds(150)));
                Assert.Equal("Temp-Pass1", Clipboard.GetText());

                WaitFor(() => !Clipboard.ContainsText(), TimeSpan.FromSeconds(3));
                Assert.False(Clipboard.ContainsText());

                // something copied after the password is left alone
                Assert.True(ClipboardHelper.CopySensitiveText("Temp-Pass2", TimeSpan.FromMilliseconds(150)));
                Clipboard.SetText("user's own text");

                WaitFor(() => false, TimeSpan.FromMilliseconds(400));
                Assert.Equal("user's own text", Clipboard.GetText());
            }
            finally
            {
                if (before is null) Clipboard.Clear(); else Clipboard.SetText(before);
            }
        });
    }

    // pumps the dispatcher (so DispatcherTimers tick) until the condition holds or the time is up
    private static void WaitFor(Func<bool> condition, TimeSpan timeout)
    {
        var until = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < until && !condition())
        {
            var frame = new DispatcherFrame();

            var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(20), DispatcherPriority.Background, (s, _) =>
            {
                ((DispatcherTimer)s!).Stop();
                frame.Continue = false;
            }, Dispatcher.CurrentDispatcher);

            Dispatcher.PushFrame(frame);
        }
    }
}
