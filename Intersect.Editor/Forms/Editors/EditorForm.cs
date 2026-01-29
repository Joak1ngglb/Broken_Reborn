using System.ComponentModel;
using DarkUI.Controls;
using Intersect.Editor.Core;
using Intersect.Editor.General;
using Intersect.Editor.Networking;
using Intersect.Enums;
using Microsoft.Extensions.Logging;


namespace Intersect.Editor.Forms.Editors;


public partial class EditorForm : Form
{

    private bool mClosing = false;

    protected DarkButton? _btnSave;
    protected DarkButton? _btnCancel;
    protected DarkButton? _btnTranslate;

    protected EditorForm()
    {
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
        {
            return;    // Evita ejecutar código que depende del runtime
        }

        Icon = Program.Icon;
        ApplyHooks();
    }

    protected void ApplyHooks()
    {
        PacketHandler.GameObjectUpdatedDelegate = type =>
        {
            if (IsDisposed || mClosing || Disposing)
            {
                return;
            }

            var action = (Action<GameObjectType>) FireGameObjectUpdatedDelegate;
            try
            {
                if (!this.Disposing && !this.IsDisposed)
                {
                    if (InvokeRequired)
                    {
                        Invoke(action, type);
                    }
                    else
                    {
                        action(type);
                    }
                }
            }
            catch (Exception e)
            {
                Intersect.Core.ApplicationContext.Context.Value?.Logger.LogDebug(e, "Error updating game object");
            }
        };

        this.Closing += EditorForm_Closing;
    }

    private void EditorForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        mClosing = true;
    }

    private void FireGameObjectUpdatedDelegate(GameObjectType type)
    {
        if (IsDisposed || mClosing || Disposing)
        {
            return;
        }

        GameObjectUpdatedDelegate(type);
    }

    protected virtual void GameObjectUpdatedDelegate(GameObjectType type)
    {
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        //
        // EditorForm
        //
        this.ClientSize = new System.Drawing.Size(284, 261);
        this.Name = "EditorForm";
        this.ResumeLayout(false);

    }

    protected void UpdateEditorButtons(bool isItemSelected)
    {
        if (_btnSave != null)
        {
            _btnSave.Visible = isItemSelected;
            _btnSave.Enabled = isItemSelected;
        }

        if (_btnCancel != null)
        {
            _btnCancel.Visible = isItemSelected;
            _btnCancel.Enabled = isItemSelected;
        }

        if (_btnTranslate != null)
        {
            _btnTranslate.Visible = isItemSelected;
            _btnTranslate.Enabled = isItemSelected;
        }
    }

    protected void OpenTranslationWorkbench(string? entityType, Guid entityId)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            return;
        }

        if (Globals.MainForm != null)
        {
            Globals.MainForm.OpenTranslationWorkbench(entityType, entityId);
            return;
        }

        var workbench = new FrmTranslationWorkbench(entityType, entityId.ToString())
        {
            Owner = this,
        };
        workbench.Show(this);
    }
}
