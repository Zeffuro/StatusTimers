using System;
using System.Numerics;
using Dalamud.Game.Text.SeStringHandling;
using KamiToolKit;
using KamiToolKit.Nodes;
using KamiToolKit.Classes;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Windows;

/// <summary>
/// A modal dialog for Yes/No questions, attachable via NativeController.AttachNode.
/// </summary>
public unsafe class ModalNode
{
    // Root node for the modal
    public ResNode RootNode { get; } = new()
    {
        Width = 340,
        Height = 140,
        Position = new Vector2(20, 20),
        IsVisible = false,
    };

    private readonly ResNode _background;
    private readonly TextNode _message;
    private readonly TextButtonNode _yesButton;
    private readonly TextButtonNode _noButton;

    public Action<bool>? OnResult { get; set; }

    public ModalNode()
    {
        // Background
        _background = new ResNode
        {
            Width = 340,
            Height = 140,
            IsVisible = true,
        };
        // Attach background to root
        GlobalServices.NativeController.AttachNode(_background, RootNode);

        // Message
        _message = new TextNode
        {
            Width = 300,
            Height = 60,
            FontSize = 16,
            Position = new Vector2(20, 35),
            IsVisible = true
        };
        GlobalServices.NativeController.AttachNode(_message, RootNode);

        // Yes Button
        _yesButton = new TextButtonNode
        {
            Width = 100,
            Height = 28,
            Position = new Vector2(35, 100),
            Label = "Yes",
            IsVisible = true,
            IsEnabled = true
        };
        _yesButton.OnClick = () => Confirm(true);
        GlobalServices.NativeController.AttachNode(_yesButton, RootNode);

        // No Button
        _noButton = new TextButtonNode
        {
            Width = 100,
            Height = 28,
            Position = new Vector2(205, 100),
            Label = "No",
            IsVisible = true,
            IsEnabled = true
        };
        _noButton.OnClick = () => Confirm(false);
        GlobalServices.NativeController.AttachNode(_noButton, RootNode);
    }

    public string Message
    {
        get => _message.Text.ToString();
        set => _message.Text = value;
    }

    public void Show(string message, Action<bool> callback)
    {
        Message = message;
        OnResult = callback;
        RootNode.IsVisible = true;
    }

    public void Hide()
    {
        RootNode.IsVisible = false;
    }

    private void Confirm(bool result)
    {
        GlobalServices.Logger.Info($"Confirmed {result}");
        Hide();
        OnResult?.Invoke(result);
    }
}
