using ProjetDelta.Systemes.UIJeu;
using ProjetDelta.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetDelta.Logger
{
    public class CombatLogger : ICombatLogger
    {
        private List<CombatDataItem> _logs;

        public CombatLogger(List<CombatDataItem> logs)
        {
            _logs = logs;
        }

        public void Log(CombatDataItem item)
        {
            _logs.Add(item);
        }
       
        public void AfficherAvecDelais(IUI ui)
        {
            int count = _logs.Count;
            int baseDelay = 700; // delai constant max pour les courts combats
            int minDelay = 10;
            double maxTotalTime = 25000;
            double sumDelays = 0;
            int seuilAcceleration = 20; // seuil pour commencer l’acceleration

            double[] delays = new double[count];
            for (int i = 0; i < count; i++)
            {
                double localBase = baseDelay;
                double localMin = minDelay;
                if (_logs[i].Event == TypeEvenement.Artefact)
                {
                    localBase *= 0.25; // artefacts beaucoup plus rapides
                    localMin = Math.Max(5, minDelay / 2);
                }

                if (count <= seuilAcceleration)
                {
                    // Pas d'acceleration : delai constant pour chaque coup
                    delays[i] = localBase;
                }
                else
                {
                    double progress = (double)i / (count - 1);
                    double accelerationPower = 10;
                    double factor = progress == 1 ? 1 : 1 - Math.Pow(2, -accelerationPower * progress);

                    delays[i] = localBase - factor * (localBase - localMin);
                }
                sumDelays += delays[i];
            }

            double scalingFactor = 1.0;
            if (sumDelays > maxTotalTime)
            {
                scalingFactor = maxTotalTime / sumDelays;
            }

            UIJeuSendConsole uijeu = new UIJeuSendConsole(ui);

            for (int i = 0; i < count; i++)
            {
                uijeu.AfficherCombatItem(_logs[i]);

                int scaledDelay = (int)(delays[i] * scalingFactor);
                if (scaledDelay > baseDelay)
                    scaledDelay = baseDelay;

                Thread.Sleep(scaledDelay);
            }
        }
    }

}
