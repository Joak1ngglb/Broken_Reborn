using Intersect.Editor.Localization;
using Intersect.Config;
using Intersect.Framework.Core.GameObjects.Events.Commands;

namespace Intersect.Editor.Forms.Editors.Events.Event_Commands
{
    public partial class EventCommandGiveJobExperience : UserControl
    {
        private readonly FrmEvent mEventEditor;
        private readonly GiveJobExperienceCommand mMyCommand;

        private JobType selectedJob;
        private long selectedExperience;

        // Diccionario para mapear los índices del ComboBox con los valores del JobType
        private readonly Dictionary<int, JobType> ComboBoxJobMapping = new Dictionary<int, JobType>();

        public EventCommandGiveJobExperience(GiveJobExperienceCommand refCommand, FrmEvent editor)
        {
            InitializeComponent();

            mMyCommand = refCommand;
            mEventEditor = editor;

            LoadExistingValues();
            InitializeComboBox();

            // Cargar localización
            InitLocalization();
        }

        private void InitLocalization()
        {
            grpGiveExperience.Text = Strings.EventGiveExperience.Title;
          
          
        }

        private void InitializeComboBox()
        {
            cmbJob.Items.Clear();
            ComboBoxJobMapping.Clear();

            cmbJob.Items.Add("-- Select a Job --");
            ComboBoxJobMapping[0] = JobType.None;

            int comboIndex = 1;

            for (int i = 1; i < (int)JobType.JobCount; i++)
            {
                JobType job = (JobType)i;
                ComboBoxJobMapping[comboIndex] = job;
                cmbJob.Items.Add(General.Globals.GetJobName(i));
                comboIndex++;
            }

            cmbJob.SelectedIndex = FindComboIndexForJob(selectedJob);
            nudExperience.Value = selectedExperience;
        }

        private void UpdateCommandPrinter()
        {
            string commandText = GetCommandText();
            //printerCommand.Text = commandText; // Actualizar el texto del comando en la interfaz
        }

        private string GetCommandText()
        {
            return $"Give {selectedExperience} EXP to {General.Globals.GetJobName((int)selectedJob)}";
        }

        private void LoadExistingValues()
        {
            selectedJob = JobType.None;
            selectedExperience = 0;

            if (mMyCommand.JobExp == null || mMyCommand.JobExp.Count == 0)
            {
                return;
            }

            var configuredValue = mMyCommand.JobExp
                .FirstOrDefault(x => x.Key != JobType.None && x.Key != JobType.JobCount && x.Value != 0);

            if (!configuredValue.Equals(default(KeyValuePair<JobType, long>)))
            {
                selectedJob = configuredValue.Key;
                selectedExperience = configuredValue.Value;

                return;
            }

            var existingValue = mMyCommand.JobExp
                .FirstOrDefault(x => x.Key != JobType.None && x.Key != JobType.JobCount);

            if (!existingValue.Equals(default(KeyValuePair<JobType, long>)))
            {
                selectedJob = existingValue.Key;
                selectedExperience = existingValue.Value;
            }
        }

        private int FindComboIndexForJob(JobType job)
        {
            foreach (var pair in ComboBoxJobMapping)
            {
                if (pair.Value == job)
                {
                    return pair.Key;
                }
            }

            return 0;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            long expAmount = (long)nudExperience.Value;

            if (!ComboBoxJobMapping.TryGetValue(cmbJob.SelectedIndex, out var mappedJob))
            {
                MessageBox.Show("Please select a valid job.");
                return;
            }

            if (mappedJob == JobType.None)
            {
                MessageBox.Show("Please select a valid job.");
                return;
            }

            // Limpiar la experiencia antes de asignar la nueva
            mMyCommand.JobExp.Clear(); // <-- Esto asegurará que solo se guarde la nueva experiencia

            // Guardar los datos en el comando
            mMyCommand.JobExp[mappedJob] = expAmount;
            selectedJob = mappedJob;
            selectedExperience = expAmount;

            // Actualizar el texto en la UI del evento
            UpdateCommandPrinter();

            // Guardar cambios
            mEventEditor.FinishCommandEdit();
        }


        private void btnCancel_Click(object sender, EventArgs e)
        {
            mEventEditor.CancelCommandEdit();
        }

        private void nudExperience_ValueChanged(object sender, EventArgs e)
        {
            selectedExperience = (long)nudExperience.Value;

            // Si ya hay una entrada para el trabajo seleccionado, actualizarla
            if (mMyCommand.JobExp.ContainsKey(selectedJob))
            {
                mMyCommand.JobExp[selectedJob] = selectedExperience;
            }

            // Forzar actualización del texto en UI
            UpdateCommandPrinter();
        }

        private void cmbJob_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ComboBoxJobMapping.TryGetValue(cmbJob.SelectedIndex, out var job))
            {
                selectedJob = job;
                UpdateCommandPrinter(); // Refrescar el texto del comando
            }
            else
            {
                selectedJob = JobType.None;
                MessageBox.Show("Job seleccionado no válido. Por favor, revisa el ComboBoxJobMapping.");
            }
        }


    }
}
