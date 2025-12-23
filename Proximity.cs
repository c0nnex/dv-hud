using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DV.Simulation.Cars;
using UnityEngine;

namespace DvMod.HeadsUpDisplay
{
    internal static class Proximity
    {
        public static string GetProximity(TrainCar car, bool debug = false)
        {
            var proximitySettings = Main.settings.proximitySettings;
            var bogie = car.Bogies[1];
            var track = bogie.track;
            if (track == null)
                return "No Track";

            var locoDirection = PlayerManager.LastLoco == null || PlayerManager.LastLoco.GetComponent<SimController>()?.controlsOverrider.Reverser.Value >= 0.5f;
            var direction = !locoDirection ^ (bogie.TrackDirectionSign > 0);
            var ignoreSet = car.trainset?.id ?? int.MinValue;

            var refCar = car.CarAtEnd(direction);

            var targetTrack = refCar.logicCar?.CurrentTrack;
            if (targetTrack == null) return "On Junction";

            var playerCoupler = refCar.GetCoupler(direction);// car.trainset?.GetEndmost(direction);
            if (playerCoupler == null)
                return "No Coupler!!";
            Vector3 playerPosition = playerCoupler.transform.position; // Loco or Last Car depending on direction;
            var startSpan = playerCoupler.train.Bogies[1].traveller.Span;
            Main.DebugLog(() => $"CARCHECK START car {car.ID} set {ignoreSet} dir {direction} track {targetTrack.ID} refSel {playerCoupler.train.ID}");
            var allSets = Trainset.allSets.Where(t => t.id != ignoreSet).FilterByTrack(targetTrack, direction, startSpan, playerPosition, proximitySettings.preciseSpan).OrderBy(t => t.Distance);
            var selectedSet = allSets.FirstOrDefault();
            Main.DebugLog(() => $"CARCHECK END {selectedSet}");
            if (selectedSet == null)
                return ""; // Nothing Near
            if (debug)
            {
                Main.DebugLog(new DebugHelper(targetTrack, playerPosition, startSpan).Generate(allSets), true);
                Debug.Log("Proximity data logged");
            }          
            if (selectedSet.Distance > proximitySettings.maxProximitySpan)
                return "Too far away"; // maxSpan " + selectedSet;
            return selectedSet.ToString();
        }
    }
}
