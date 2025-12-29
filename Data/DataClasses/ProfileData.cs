namespace ProjetDelta.Data.DataClasses
{
    public class ProfileData
    {
        public int MetaCurrency { get; set; } = 0;
        public int BossesDefeated { get; set; } = 0;
        public bool MetaUnlocked { get; set; } = false;
        public bool FinalBossBeaten { get; set; } = false;
        public float MetaGoldBonus { get; set; } = 0f;
        public float MetaLootBonus { get; set; } = 0f;
        public float MetaStatBonus { get; set; } = 0f; // +% stats globaux
        public int MetaBackgroundFlatBonus { get; set; } = 0; // +stats de base sur backgrounds
        public float MetaClasseMultBonus { get; set; } = 0f; // +% sur multiplicateurs de classe
        public List<string> MetaArtefacts { get; set; } = new(); // Artefacts communs permanents pour nouveaux persos
        public int HighestDifficultyUnlocked { get; set; } = 0; // 0 = Facile, 1 = Normal, 2 = Difficile, ...
        public int BossTestLevelUnlocked { get; set; } = 0; // niveau de test du boss disponible
        public int BossTestBestLevel { get; set; } = 0; // meilleur niveau atteint
    }
}
