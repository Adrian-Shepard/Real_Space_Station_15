namespace SS15.Client.Systems
{
    public class AtmosphereData
    {
        // Температура в Кельвинах
        public float Temperature = 293.15f;

        // Количество молей газов
        public float MolesOxygen = 21f;
        public float MolesNitrogen = 79f;
        public float MolesCarbonDioxide = 0.03f;
        public float MolesPlasma = 0f;      // добавлено

        // Суммарное количество вещества
        public float TotalMoles => MolesOxygen + MolesNitrogen + MolesCarbonDioxide + MolesPlasma;

        // Процентное содержание газов
        public float OxygenPercent => TotalMoles > 0 ? MolesOxygen / TotalMoles * 100f : 0f;
        public float NitrogenPercent => TotalMoles > 0 ? MolesNitrogen / TotalMoles * 100f : 0f;
        public float CarbonDioxidePercent => TotalMoles > 0 ? MolesCarbonDioxide / TotalMoles * 100f : 0f;
        public float PlasmaPercent => TotalMoles > 0 ? MolesPlasma / TotalMoles * 100f : 0f;

        // Давление (кПа) = n * R * T / V (V = 1)
        public float Pressure => TotalMoles * 0.003455f * Temperature;
    }
}