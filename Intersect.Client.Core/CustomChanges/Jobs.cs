using Intersect.Client.Networking;
using Intersect.Config;
using Intersect.Network.Packets.Server;


namespace Intersect.Client.Entities;

public partial class Player
{
    // Inicializa los diccionarios al crear el jugador
    public Dictionary<JobType, int> JobLevel { get; set; } = [];
    public Dictionary<JobType, long> JobExp { get; set; } = [];
    public Dictionary<JobType, long> JobExpToNextLevel { get; set; } = [];
    public Dictionary<JobType, int> UnspentJobPoints { get; set; } = [];
    public Dictionary<JobType, int> SpentJobPoints { get; set; } = [];
    public Dictionary<JobType, Dictionary<int, int>> PurchasedJobPerks { get; set; } = [];


    public void UpdateJobsFromPacket(Dictionary<JobType, JobData> jobData)
    {
        if (jobData == null)
        {
            PacketSender.SendChatMsg("Error: El paquete de datos de trabajos está vacío.", 5);
            return;
        }

        foreach (var job in jobData)
        {
            var jobType = job.Key;
            var jobDetails = job.Value;

            // Asegurar inicialización
            if (!JobLevel.ContainsKey(jobType))
            {
                JobLevel[jobType] = 1;
            }

            if (!JobExp.ContainsKey(jobType))
            {
                JobExp[jobType] = 0;
            }

            if (!JobExpToNextLevel.ContainsKey(jobType))
            {
                JobExpToNextLevel[jobType] = 100;
            }

            if (!UnspentJobPoints.ContainsKey(jobType))
            {
                UnspentJobPoints[jobType] = 0;
            }

            if (!SpentJobPoints.ContainsKey(jobType))
            {
                SpentJobPoints[jobType] = 0;
            }

            if (!PurchasedJobPerks.ContainsKey(jobType))
            {
                PurchasedJobPerks[jobType] = new Dictionary<int, int>();
            }

            // Actualizar valores
            JobLevel[jobType] = jobDetails.Level;
            JobExp[jobType] = jobDetails.Experience;
            JobExpToNextLevel[jobType] = jobDetails.ExperienceToNextLevel;
            UnspentJobPoints[jobType] = Math.Max(0, jobDetails.UnspentJobPoints);
            SpentJobPoints[jobType] = Math.Max(0, jobDetails.SpentJobPoints);
            PurchasedJobPerks[jobType] = jobDetails.PurchasedPerks != null
                ? new Dictionary<int, int>(jobDetails.PurchasedPerks)
                : new Dictionary<int, int>();

            // Depuración en el cliente
            //  PacketSender.SendChatMsg($"Trabajo {jobType} actualizado: Nivel {jobDetails.Level}, Exp {jobDetails.Experience}/{jobDetails.ExperienceToNextLevel}", 5);
        }
    }


}
