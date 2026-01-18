using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Admin.Actions;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Control.Layout;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Framework.Core.Descriptors;
using Intersect.Framework.Core.GameObjects.Items;
using static Intersect.Client.Framework.File_Management.GameContentManager;

namespace Intersect.Client.Interface.Game.Admin;

public sealed class AdminMailBroadcastWindow : Window
{
    private const int MailAttachmentSlotCount = BroadcastMailAction.MaxAttachments;

    private readonly IFont? _defaultFont;
    private readonly TextBox _subjectInput;
    private readonly MultilineTextBox _messageInput;
    private readonly LabeledComboBox[] _attachmentDropdowns;
    private readonly TextBoxNumeric[] _attachmentQuantityInputs;
    private readonly LabeledCheckBox _onlineOnlyCheckbox;
    private readonly Button _sendButton;
    private bool _localizationSubscribed;

    public AdminMailBroadcastWindow() : base(
        Interface.GameUi.GameCanvas,
        Strings.AdminWindow.MailBroadcast,
        false,
        nameof(AdminMailBroadcastWindow)
    )
    {
        _defaultFont = Skin?.DefaultFont ?? Current.GetFont(TitleLabel.FontName);

        IsResizable = false;
        MinimumSize = new Point(520, 520);
        InnerPanelPadding = new Padding(8);
        InnerPanel.DockChildSpacing = new Padding(0, 8, 0, 0);

        _attachmentDropdowns = new LabeledComboBox[MailAttachmentSlotCount];
        _attachmentQuantityInputs = new TextBoxNumeric[MailAttachmentSlotCount];

        var contentPanel = new Panel(this, "ContentPanel")
        {
            Dock = Pos.Fill,
            ShouldDrawBackground = false,
            DockChildSpacing = new Padding(0, 8, 0, 0),
        };

        _ = new Label(contentPanel, "SubjectLabel")
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Text = Strings.AdminWindow.MailSubject,
        };

        _subjectInput = new TextBox(contentPanel, nameof(_subjectInput))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
        };
        Interface.FocusComponents.Add(_subjectInput);
        _subjectInput.TextChanged += (_, _) => UpdateActionControls();

        _ = new Label(contentPanel, "MessageLabel")
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Text = Strings.AdminWindow.MailMessage,
        };

        _messageInput = new MultilineTextBox(contentPanel)
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Height = 200,
        };
        _messageInput.Name = nameof(_messageInput);
        Interface.FocusComponents.Add(_messageInput);
        _messageInput.TextChanged += (_, _) => UpdateActionControls();

        var attachmentsContainer = new Panel(contentPanel, "AttachmentsContainer")
        {
            Dock = Pos.Top,
            ShouldDrawBackground = false,
            DockChildSpacing = new Padding(0, 8, 0, 0),
        };

        for (var index = 0; index < MailAttachmentSlotCount; index++)
        {
            var attachmentPanel = new Panel(attachmentsContainer, $"Attachment{index}")
            {
                Dock = Pos.Top,
                ShouldDrawBackground = false,
            };

            var dropdown = new LabeledComboBox(attachmentPanel, $"AttachmentDropdown{index}")
            {
                Dock = Pos.Top,
                Font = _defaultFont,
                FontSize = 12,
                Label = Strings.AdminWindow.MailAttachmentItem.ToString(index + 1),
                TextPadding = new Padding(8, 4, 0, 4),
            };
            PopulateItemDropdown(dropdown);
            dropdown.ItemSelected += (_, _) => UpdateActionControls();

            var quantityPanel = new Panel(attachmentPanel, $"AttachmentQuantityPanel{index}")
            {
                Dock = Pos.Top,
                ShouldDrawBackground = false,
                Margin = new Margin(0, 4, 0, 0),
            };

            _ = new Label(quantityPanel, $"AttachmentQuantityLabel{index}")
            {
                Dock = Pos.Left,
                Font = _defaultFont,
                FontSize = 12,
                Margin = new Margin(0, 0, 4, 0),
                Text = Strings.AdminWindow.MailAttachmentQuantity.ToString(index + 1),
                TextAlign = Pos.Left | Pos.CenterV,
            };

            var quantityInput = new TextBoxNumeric(quantityPanel, $"AttachmentQuantityInput{index}")
            {
                Dock = Pos.Fill,
                Font = _defaultFont,
                FontSize = 12,
                Padding = new Padding(8, 4),
            };
            quantityInput.SetRange(1, int.MaxValue);
            quantityInput.Value = 1;
            quantityInput.ValueChanged += (_, _) => UpdateActionControls();
            Interface.FocusComponents.Add(quantityInput);

            _attachmentDropdowns[index] = dropdown;
            _attachmentQuantityInputs[index] = quantityInput;
        }

        _onlineOnlyCheckbox = new LabeledCheckBox(contentPanel, nameof(_onlineOnlyCheckbox))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Text = Strings.AdminWindow.MailOnlineOnly,
        };

        var buttonsPanel = new Panel(contentPanel, "ButtonsPanel")
        {
            Dock = Pos.Top,
            ShouldDrawBackground = false,
        };

        _sendButton = new Button(buttonsPanel, nameof(_sendButton))
        {
            Dock = Pos.Left,
            Text = Strings.AdminWindow.MailSend,
        };
        StyleButton(_sendButton);
        _sendButton.Clicked += SendButtonOnClicked;

        UpdateActionControls();
        SubscribeToLocalizationUpdates();
    }

    private static Padding StdPad(int x = 8, int y = 4) => new(x, y);

    private void StyleButton(Button button)
    {
        button.MinimumSize = new Point(140, 32);
        button.Padding = StdPad();
        button.Margin = new Margin(0, 0, 8, 0);
        button.Font = _defaultFont;
        button.FontSize = 12;
    }

    private void PopulateItemDropdown(LabeledComboBox dropdown)
    {
        var selectedItemId = dropdown.SelectedItem?.UserData as Guid?;
        dropdown.ClearItems();

        var noneItem = dropdown.AddItem(Strings.AdminWindow.None, userData: Guid.Empty);

        foreach (var descriptor in ItemDescriptor.Lookup.Values
                     .OfType<ItemDescriptor>()
                     .OrderBy(
                         descriptor => GetLocalizedItemName(descriptor),
                         StringComparer.CurrentCultureIgnoreCase
                     ))
        {
            if (descriptor == null)
            {
                continue;
            }

            var displayName = GetLocalizedItemName(descriptor);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = descriptor.Id.ToString();
            }

            _ = dropdown.AddItem(displayName, userData: descriptor.Id);
        }

        dropdown.SelectedItem = noneItem;
        if (selectedItemId is { } selectedId)
        {
            var selectedItem = dropdown.Items.FirstOrDefault(item => item.UserData is Guid id && id == selectedId);
            if (selectedItem != null)
            {
                dropdown.SelectedItem = selectedItem;
            }
        }

        RequestLocalizationEntries();
    }

    private static string GetLocalizedItemName(ItemDescriptor descriptor) =>
        GameLocalization.GetTextOrDefault(
            descriptor.Type.ToString(),
            descriptor.Id,
            "Name",
            descriptor.Name ?? string.Empty
        );

    private void RequestLocalizationEntries()
    {
        var requests = new List<LocalizationRequestEntry>();
        foreach (var descriptor in ItemDescriptor.Lookup.Values.OfType<ItemDescriptor>())
        {
            if (descriptor == null)
            {
                continue;
            }

            requests.Add(new LocalizationRequestEntry(descriptor.Type.ToString(), descriptor.Id.ToString(), "Name"));
        }

        if (requests.Count > 0)
        {
            GameLocalization.RequestEntries(requests);
        }
    }

    private void SubscribeToLocalizationUpdates()
    {
        if (_localizationSubscribed)
        {
            return;
        }

        GameLocalization.LocalizedTextsUpdated += OnLocalizedTextsUpdated;
        Disposed += (_, _) => UnsubscribeFromLocalizationUpdates();
        _localizationSubscribed = true;
    }

    private void UnsubscribeFromLocalizationUpdates()
    {
        if (!_localizationSubscribed)
        {
            return;
        }

        GameLocalization.LocalizedTextsUpdated -= OnLocalizedTextsUpdated;
        _localizationSubscribed = false;
    }

    private void OnLocalizedTextsUpdated(string language, IReadOnlyCollection<LocalizationRequestEntry> requests)
    {
        var itemType = GameObjectType.Items.ToString();
        if (!requests.Any(request => request.EntityType == itemType && request.Field == "Name"))
        {
            return;
        }

        foreach (var dropdown in _attachmentDropdowns)
        {
            PopulateItemDropdown(dropdown);
        }
    }

    private void SendButtonOnClicked(Base sender, MouseButtonState args)
    {
        var subject = _subjectInput.Text?.Trim() ?? string.Empty;
        var message = _messageInput.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        PacketSender.SendAdminAction(
            new BroadcastMailAction(
                subject,
                message,
                CollectAttachments(),
                _onlineOnlyCheckbox.IsChecked
            )
        );
    }

    private List<BroadcastMailAttachment> CollectAttachments()
    {
        var attachments = new List<BroadcastMailAttachment>();

        for (var index = 0; index < _attachmentDropdowns.Length; index++)
        {
            var dropdown = _attachmentDropdowns[index];
            var quantityInput = _attachmentQuantityInputs[index];

            if (!(dropdown?.SelectedItem?.UserData is Guid itemId) || itemId == Guid.Empty)
            {
                continue;
            }

            var quantity = (int)Math.Max(1, Math.Round(quantityInput?.Value ?? 0));
            if (quantity <= 0)
            {
                continue;
            }

            attachments.Add(new BroadcastMailAttachment(itemId, quantity));
        }

        return attachments;
    }

    private void UpdateActionControls()
    {
        var hasSubject = !string.IsNullOrWhiteSpace(_subjectInput.Text);
        var hasMessage = !string.IsNullOrWhiteSpace(_messageInput.Text);
        var attachmentsValid = true;

        for (var index = 0; index < _attachmentDropdowns.Length; index++)
        {
            var dropdown = _attachmentDropdowns[index];
            var quantityInput = _attachmentQuantityInputs[index];

            if (!(dropdown?.SelectedItem?.UserData is Guid attachmentItemId) || attachmentItemId == Guid.Empty)
            {
                continue;
            }

            if ((quantityInput?.Value ?? 0) < 1)
            {
                attachmentsValid = false;
                break;
            }
        }

        _sendButton.IsDisabled = !(hasSubject && hasMessage && attachmentsValid);
    }

    protected override void EnsureInitialized()
    {
        LoadJsonUi(UI.InGame, Graphics.Renderer?.GetResolutionString(), saveOutput: true);
    }
}
