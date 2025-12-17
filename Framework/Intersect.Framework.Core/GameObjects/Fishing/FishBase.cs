using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Framework.Core.GameObjects.Conditions;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Fishing;

public partial class FishBase : DatabaseObject<FishBase>, IFolderable
{
    [JsonConstructor]
    public FishBase(Guid id) : base(id)
    {
        Name = "New Fish";
    }

    //Parameterless constructor for EF
    public FishBase()
    {
        Name = "New Fish";
    }

    //{ get; set; }
    //Øàíñ ïîéìàòü ðûáêó
    [JsonProperty(Order = -13)]
    public int chance { get; set; } = 100;
    //Êàê ÷àñòî ïðèä¸òñÿ äîëáèòü êíîïêó
    [JsonProperty(Order = -12)]
    public int weight { get; set; } = 20;
    //Äåðçîñòü ðûâêà êðþ÷êà â íà÷àëå ðûáàëêè
    [JsonProperty(Order = -11)]
    public int pushStrength { get; set; } = 0;
    //ñèëà âûðûâàíèå êðþ÷êà â ïðîöåññå ðûáàëêè
    [JsonProperty(Order = -10)]
    public int strength { get; set; } = 20;
    //Ïîçèöèÿ íà ïîëçóíêå â íà÷àëå ðûáàëêè
    [JsonProperty(Order = -9)]
    public int position { get; set; } = 0;

    //Ðàçìåð äèàïàçîíà ëîâëè ðûáû
    [JsonProperty(Order = -8)]
    public int rangeSize { get; set; } = 33;
    //Ñêîðîñòü ðûáû
    [JsonProperty(Order = -7)]
    public int speedMove { get; set; } = 20;
    //Ìàí¸âðåííîñòü ðûáû. Òî, íàñêîëüêî áûñòðî áóäåò ìåíÿòüñÿ äèàïàçîí íà íîâûé ðàçìåð
    [JsonProperty(Order = -6)]
    public int speedChangeRangeSize { get; set; } = 50;


    //Êîýôôèöèåíò íåïðåäñêàçóåìîñòè. Íàñêîëüêî ìîãóò îòëè÷àòüñÿ ïàðàìåòðû ðûáû îò çàÿâëåííûõ.
    //Íå äà¸ò òî÷íî ïðåäñêàçàòü ïîâåäåíèå ðûáû. Òî, íàñêîëüêî ÷àñòî ìåíÿåòñÿ å¸ ïîâåäåíèå è íàñêîëüêî.
    //Ïðè 100 ïîâåäåíèå ðûáû áóäåò ìåíÿòüñÿ ÷àùå ñ îãðîìíûìè îòëè÷èÿìè äðóã îò äðóãà
    //Ïðè 0 îíà áóäåò âåñòè ñåáÿ ñòðîãî ñ ïàðàìåòðàìè
    [JsonProperty(Order = -5)]
    public int coeffUnpredictability { get; set; } = 0;
    //Âðåìÿ ñìåíû íàïðàâëåíèÿ ðûáû
    [JsonProperty(Order = -4)]
    public int timeChangeSpeed { get; set; } = 2000;
    //Âðåìÿ ñìåíû äèàïàçîíà ðûáû
    [JsonProperty(Order = -3)]
    public int timeChangeRangeSize { get; set; } = 5000;

    //Ïðåäìåò ðûáêè
    [JsonProperty(Order = -2)]
    public Guid ItemId { get; set; }

    /// <inheritdoc />
    public string Folder { get; set; } = "";

    //Èâåíò ðûáêè
    [Column("Event")]
    [JsonProperty]
    public Guid EventId { get; set; }

    [NotMapped]
    [JsonIgnore]
    public EventDescriptor Event
    {
        get => EventDescriptor.Get(EventId);
        set => EventId = value?.Id ?? Guid.Empty;
    }

    [NotMapped]
    public ConditionLists FishingRequirements = new ConditionLists();

    //Òðåáîâàíèÿ äëÿ ëîâëè ýòîé ðûáêè
    [Column("FishingRequirements")]
    [JsonIgnore]
    public string JsonFishingRequirements
    {
        get => FishingRequirements.Data();
        set => FishingRequirements.Load(value ?? "[]");
    }
}

