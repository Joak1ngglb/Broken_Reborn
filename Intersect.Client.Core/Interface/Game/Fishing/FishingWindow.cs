using System;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Interface;
using Intersect.Client.Localization;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.GameObjects;

namespace Intersect.Client.Interface.Game.Fishing;

public partial class FishingWindow : Window
{
    private ImagePanel? _root;

    private ImagePanel? _fishEventPanel;
    private ImagePanel? _rangeMoveFrame;
    private ImagePanel? _rangeMoveAnchorFiller;
    private ImagePanel? _rangeMoveFiller;

    private ImagePanel? _playerHook;

    private ImagePanel? _progressBarFrame;
    private ImagePanel? _progressBarAnchorFiller;
    private ImagePanel? _progressBarFiller;

    private ImagePanel? _success;
    private ImagePanel? _itemViewer;
    private ImagePanel? _failed;
    private Label? _successText;

    public FishingWindow(Canvas gameCanvas) : base(gameCanvas, Strings.Fishing.WindowTitle, false, nameof(FishingWindow))
    {
        DisableResizing();
        IsClosable = false;
        IsResizable = false;
        SkipRender();

        Alignment = [Alignments.Center];

        Hide();
    }

    protected override void EnsureInitialized()
    {
        if (_root != null)
        {
            return;
        }

        BuildUi();
        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
    }

    private void BuildUi()
    {
        _root = new ImagePanel(this, "FishEventUI")
        {
            RestrictToParent = false,
            MouseInputEnabled = false,
            ShouldDrawBackground = true,
            Alignment = [Alignments.Center],
            RenderColor = Color.FromArgb(255, 255, 255, 255),
            MinimumSize = new Point(1, 1),
            MaximumSize = new Point(4096, 4096),
        };
        _root.SetBounds(0, 0, 512, 168);

        _fishEventPanel = new ImagePanel(_root, "MainPanel")
        {
            RestrictToParent = false,
            MouseInputEnabled = false,
            ShouldDrawBackground = true,
            Alignment = [Alignments.Center],
            TextureFilename = "fish_background.png",
            RenderColor = Color.FromArgb(255, 255, 255, 255),
            MinimumSize = new Point(1, 1),
            MaximumSize = new Point(4096, 4096),
        };
        _fishEventPanel.SetBounds(0, 0, 512, 168);

        _rangeMoveFrame = new ImagePanel(_fishEventPanel, "MoveRange")
        {
            RestrictToParent = true,
            MouseInputEnabled = false,
            ShouldDrawBackground = true,
            Alignment = [Alignments.Center],
            TextureFilename = "fish_backlayer.png",
            RenderColor = Color.FromArgb(255, 255, 255, 255),
            MinimumSize = new Point(1, 1),
            MaximumSize = new Point(4096, 4096),
        };
        _rangeMoveFrame.SetBounds(24, 96, 464, 48);

        _rangeMoveAnchorFiller = new ImagePanel(_rangeMoveFrame, "rangeMoveAnchorFiller")
        {
            RestrictToParent = true,
            MouseInputEnabled = false,
            ShouldDrawBackground = true,
            Alignment = [Alignments.Center],
            RenderColor = Color.FromArgb(255, 255, 255, 255),
            MinimumSize = new Point(1, 1),
            MaximumSize = new Point(4096, 4096),
        };
        _rangeMoveAnchorFiller.SetBounds(0, 0, 464, 48);

        _rangeMoveFiller = new ImagePanel(_rangeMoveFrame, "MoveFiller")
        {
            RestrictToParent = true,
            MouseInputEnabled = false,
            ShouldDrawBackground = true,
            Alignment = [Alignments.Center],
            TextureFilename = "fish_filler.png",
            RenderColor = Color.FromArgb(255, 255, 255, 255),
            MinimumSize = new Point(1, 1),
            MaximumSize = new Point(4096, 4096),
        };
        _rangeMoveFiller.SetBounds(198, 2, 68, 44);

        _playerHook = new ImagePanel(_rangeMoveFrame, "MoveForce")
        {
            RestrictToParent = true,
            MouseInputEnabled = false,
            ShouldDrawBackground = true,
            Alignment = [Alignments.Center],
            TextureFilename = "fish_force.png",
            RenderColor = Color.FromArgb(255, 255, 255, 255),
            MinimumSize = new Point(1, 1),
            MaximumSize = new Point(4096, 4096),
        };
        _playerHook.SetBounds(208, 0, 48, 48);

        _progressBarFrame = new ImagePanel(_fishEventPanel, "ProgressBarFrame")
        {
            RestrictToParent = true,
            MouseInputEnabled = false,
            ShouldDrawBackground = true,
            Alignment = [Alignments.Center],
            TextureFilename = "fish_test1.png",
            RenderColor = Color.FromArgb(255, 255, 255, 255),
            MinimumSize = new Point(1, 1),
            MaximumSize = new Point(4096, 4096),
        };
        _progressBarFrame.SetBounds(24, 24, 464, 48);

        _progressBarAnchorFiller = new ImagePanel(_progressBarFrame, "ProgressBarAnchorFiller")
        {
            RestrictToParent = true,
            MouseInputEnabled = false,
            ShouldDrawBackground = true,
            Alignment = [Alignments.Center],
            RenderColor = Color.FromArgb(255, 255, 255, 255),
            MinimumSize = new Point(1, 1),
            MaximumSize = new Point(4096, 4096),
        };
        _progressBarAnchorFiller.SetBounds(0, 0, 464, 48);

        _progressBarFiller = new ImagePanel(_progressBarFrame, "ProgressBarFiller")
        {
            RestrictToParent = true,
            MouseInputEnabled = false,
            ShouldDrawBackground = true,
            Alignment = [Alignments.Center],
            TextureFilename = "fish_test2.png",
            RenderColor = Color.FromArgb(255, 255, 255, 255),
            MinimumSize = new Point(1, 1),
            MaximumSize = new Point(4096, 4096),
        };
        _progressBarFiller.SetBounds(0, 0, 464, 48);

        _success = new ImagePanel(_root, "Success")
        {
            RestrictToParent = true,
            MouseInputEnabled = false,
            ShouldDrawBackground = true,
            Alignment = [Alignments.Center],
            TextureFilename = "fish_sussess.png",
            RenderColor = Color.FromArgb(255, 255, 255, 255),
            MinimumSize = new Point(1, 1),
            MaximumSize = new Point(4096, 4096),
        };
        _success.SetBounds(160, 0, 192, 168);

        _itemViewer = new ImagePanel(_success, "ItemViewer")
        {
            RestrictToParent = true,
            MouseInputEnabled = false,
            ShouldDrawBackground = true,
            Alignment = [Alignments.Center],
            RenderColor = Color.FromArgb(255, 255, 255, 255),
            MinimumSize = new Point(1, 1),
            MaximumSize = new Point(4096, 4096),
        };
        _itemViewer.SetBounds(72, 48, 48, 48);

        _successText = new Label(_success, "SuccessText")
        {
            Text = "You got the fish!",
            RenderColor = Color.FromArgb(255, 255, 255, 255),
            Alignment = [Alignments.Center],
            ShouldDrawBackground = true,
            MinimumSize = new Point(1, 1),
            MaximumSize = new Point(4096, 4096),
            TextColor = new Color(255, 255, 255, 255),
            FontName = "sourcesansproblack",
            FontSize = 12,
        };
        _successText.SetBounds(37, 119, 117, 20);
        _successText.SetTextScale(1f);

        _failed = new ImagePanel(_root, "Failed")
        {
            RestrictToParent = true,
            MouseInputEnabled = false,
            ShouldDrawBackground = true,
            Alignment = [Alignments.Center],
            TextureFilename = "smallbutton_clicked.png",
            RenderColor = Color.FromArgb(255, 255, 255, 255),
            MinimumSize = new Point(1, 1),
            MaximumSize = new Point(4096, 4096),
        };
        _failed.SetBounds(165, 65, 182, 38);
    }

    public bool IsVisible => IsVisibleInTree && _fishEventPanel?.IsHidden == false;

    public override void Show()
    {
        EnsureInitialized();
        base.Show();

        _root?.Show();
        _rangeMoveFrame?.Show();
        _playerHook?.Show();
        _progressBarFrame?.Show();
        _fishEventPanel?.Show();
    }

    public override void Hide()
    {
        base.Hide();
        _root?.Hide();
    }

    public void HideBars()
    {
        _rangeMoveFrame?.Hide();
        _playerHook?.Hide();
        _progressBarFrame?.Hide();
        _fishEventPanel?.Hide();
    }

    public void ShowSuccess() => _success?.Show();

    public void HideSuccess() => _success?.Hide();

    public void ShowFailed() => _failed?.Show();

    public void HideFailed() => _failed?.Hide();

    public void RedrawIcon(ItemDescriptor item)
    {
        EnsureInitialized();
        var itemTex = Globals.ContentManager.GetTexture(Framework.Content.TextureType.Item, item.Icon);
        if (_itemViewer != null)
        {
            _itemViewer.Texture = itemTex;
        }

        if (_successText != null)
        {
            _successText.Text = $"{item.Name}";
        }
    }

    public void RangeMoveResize(float valuePosition, float valueSize)
    {
        if (_rangeMoveAnchorFiller == null || _rangeMoveFiller == null)
        {
            return;
        }

        valuePosition = Math.Clamp(valuePosition, 0, 1);
        valueSize = Math.Clamp(valueSize, 0, 1);

        var width = (int)(_rangeMoveAnchorFiller.Width * valueSize);
        var x = (int)(_rangeMoveAnchorFiller.Width * valuePosition - width / 2f);

        _rangeMoveFiller.SetBounds(new Rectangle(_rangeMoveAnchorFiller.X + x, _rangeMoveAnchorFiller.Y, width, _rangeMoveFiller.Height));
    }

    public void PlayerForceMoved(float valuePosition, float valueSize)
    {
        if (_rangeMoveAnchorFiller == null || _playerHook == null)
        {
            return;
        }

        valuePosition = Math.Clamp(valuePosition, 0, 1);
        valueSize = Math.Clamp(valueSize, 0, 1);

        var width = (int)(_rangeMoveAnchorFiller.Width * valueSize);
        var x = (int)(_rangeMoveAnchorFiller.Width * valuePosition - width / 2f);

        _playerHook.SetBounds(new Rectangle(_rangeMoveAnchorFiller.X + x, _rangeMoveAnchorFiller.Y, width, _playerHook.Height));
    }

    public void Progress(float valueProgress)
    {
        if (_progressBarAnchorFiller == null || _progressBarFiller == null)
        {
            return;
        }

        valueProgress = Math.Clamp(valueProgress, 0, 1);

        var width = (int)(_progressBarAnchorFiller.Width * valueProgress);

        _progressBarFiller.SetBounds(new Rectangle(_progressBarFiller.X, _progressBarFiller.Y, width, _progressBarFiller.Height));
    }

    public void Update(Player player)
    {
        EnsureInitialized();
    }
}
