using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DV.Logic.Job;
using UnityEngine;

namespace DvMod.HeadsUpDisplay
{
    public static class TrainsetUtils
    {
        public static bool IsFacingFrontOfTrainset(TrainCar car) =>
            car.frontCoupler.IsCoupled()
                ? car.frontCoupler.coupledTo.train.indexInTrainset < car.indexInTrainset
                : car.indexInTrainset == 0;

        public static TrainCar CarAtEnd(this TrainCar loco, bool atFront) => atFront ? FirstCar(loco) : LastCar(loco);

        public static TrainCar FirstCar(this TrainCar loco) => IsFacingFrontOfTrainset(loco) ? loco.trainset.firstCar : loco.trainset.lastCar;
        public static TrainCar LastCar(this TrainCar loco) => IsFacingFrontOfTrainset(loco) ? loco.trainset.lastCar : loco.trainset.firstCar;
        public static TrainCar? GetNearestCarOnTrack(this Trainset trainset, Track track, Vector3 position)
        {
            if (trainset.cars.Count == 1)
            {
                if (trainset.firstCar.logicCar?.CurrentTrack == track)
                    return trainset.firstCar;
                return null;
            }
            var curDist = float.MaxValue;
            if (trainset.firstCar.logicCar?.CurrentTrack == track)
                curDist = Vector3.Distance(position, trainset.firstCar.transform.position);
            if (trainset.lastCar.logicCar?.CurrentTrack == track)
            {
                var thisDist = Vector3.Distance(position, trainset.lastCar.transform.position);
                if (thisDist < curDist)
                    return trainset.lastCar;
            }
            if (curDist != float.MaxValue)
                return trainset.firstCar;
            return null;
        }
        public static Coupler? GetCoupler(this TrainCar trainCar, bool front) => front ? trainCar.frontCoupler : trainCar.rearCoupler;

        public static Coupler? GetFreeCoupler(this TrainCar trainCar, Vector3 refPosition)
        {
            if (trainCar.frontCoupler.IsCoupled()) return trainCar.rearCoupler;
            if (trainCar.rearCoupler.IsCoupled()) return trainCar.frontCoupler;
            var df = Vector3.Distance(refPosition, trainCar.frontCoupler.transform.position);
            var dr = Vector3.Distance(refPosition, trainCar.rearCoupler.transform.position);
            if (df <= dr)
                return trainCar.frontCoupler;
            return trainCar.rearCoupler;
        }

        public static bool IsOnTrack(this Trainset trainset, Track tragetTrack) => trainset.cars.Any(c => c.logicCar?.CurrentTrack == tragetTrack);
        public static float OverallLength(this Trainset trainset) => trainset.cars.Sum(c => c.logicCar.length);
        public static float TotalMass(this Trainset trainset) => trainset.cars.Sum(c => c.massController.TotalMass);

        public static IEnumerable<ProximityData> FilterByTrack(this IEnumerable<Trainset> allSets, Track playerTrack, bool direction, double playerSpan, Vector3 playerPosition, double coupleDistanceStart)
        {
            foreach (var set in allSets)
            {
                var car = set.GetNearestCarOnTrack(playerTrack, playerPosition);
                if (car == null)
                {
                    Main.DebugLog(() => $"Ignore Set {set.id} Cars: {set.cars?.Count} len {set.OverallLength()} Track {set.firstCar?.logicCar?.CurrentTrack?.ID}");
                    if (set.firstCar != null)
                    {

                        Main.DebugLog(() => $"First {set.firstCar.ID}");
                        Main.DebugLog(() => $"  Front {set.firstCar.frontCoupler.IsCoupled()} {Vector3.Distance(playerPosition, set.firstCar.frontCoupler.transform.position)}m");
                        Main.DebugLog(() => $"  Rear  {set.firstCar.rearCoupler.IsCoupled()} {Vector3.Distance(playerPosition, set.firstCar.rearCoupler.transform.position)}m");
                    }
                    if (set.lastCar != null)
                    {
                        Main.DebugLog(() => $"Last {set.lastCar.ID}");
                        Main.DebugLog(() => $"  Front {set.lastCar.frontCoupler.IsCoupled()} {Vector3.Distance(playerPosition, set.lastCar.frontCoupler.transform.position)}m");
                        Main.DebugLog(() => $"  Rear  {set.lastCar.rearCoupler.IsCoupled()} {Vector3.Distance(playerPosition, set.lastCar.rearCoupler.transform.position)}m");
                    }
                    Main.DebugLog("");
                    continue;
                }
                var span = car!.Bogies[1].traveller.Span;
                var distSpan = span - playerSpan;
                if (direction && distSpan < 0)
                {
                    Main.DebugLog(() => $"Forward Set {set.id} selected {car.ID} dist {distSpan}");
                    continue;
                }
                if (!direction && distSpan > 0)
                {
                    Main.DebugLog(() => $"Backward Set {set.id} selected {car.ID} dist {distSpan}");
                    continue;
                }
                Main.DebugLog(() => $"Checking set {set.id} cars {set.cars?.Count} first {set.firstCar?.ID} last {set.lastCar?.ID} selected {car!.ID} Dist {distSpan}");
                yield return new ProximityData(set, car!.ID, span, distSpan < coupleDistanceStart ? Vector3.Distance(playerPosition, car.GetFreeCoupler(playerPosition)?.transform.position ?? Vector3.zero) : distSpan, car.isStationary);
            }
        }
    }

    public class ProximityData
    {
        public ProximityData(Trainset set, string iD, double span, double distance, bool isStationary)
        {
            Trainset = set;
            ID = iD;
            Span = span;
            Distance = distance;
            IsStationary = isStationary;
        }
        public Trainset Trainset { get; }
        public string ID { get; } = string.Empty;
        public double Span { get; } // Current relative position on Track        
        public double Distance { get; } // distance to reference (rough or exact)
        public bool IsStationary { get; }
        public override string ToString()
        {
            var str = "[" + ID + "]";
            if (IsStationary)
                str = "[<color=red>" + ID + "</color>]";
            var color = "white";
            if (Distance < 50f)
                color = Distance < 10f ? "red" : "orange";
            else if (Distance < 200f)
                color = "yellow";
            if (Distance > Main.settings.proximitySettings.preciseSpan)
                return $"{str} <color={color}>{Distance:F0} m</color>";
            return $"{str} <color={color}>{Distance:F2} m</color>";
        }
    }

    public class DebugHelper
    {
        List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>();

        Vector3 referencePosition;
        double referenceSpan;

        public DebugHelper(Track track, Vector3 referencePosition, double referenceSpan)
        {
            this.referencePosition = referencePosition;
            this.referenceSpan = referenceSpan;
            Add("ReferencePosition", referencePosition);
            Add("ReferenceSpan", referenceSpan);
            Add("ReferenceTrack", track.ID);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in data)
            {
                if (String.IsNullOrEmpty(item.Key))
                    sb.AppendLine();
                else
                    sb.AppendLine($"{item.Key}={item.Value}");
            }
            sb.AppendLine();
            return sb.ToString();
        }
        public void Add(string key, object? value)
        {
            if (value == null)
                data.Add(new KeyValuePair<string, string>(key, "NULL"));
            else
                data.Add(new KeyValuePair<string, string>(key, Convert.ToString(value, CultureInfo.InvariantCulture)));
        }

        public void Add(Coupler coupler)
        {
            string key = "Coupler.Front.";
            if (!coupler.isFrontCoupler)
                key = "Coupler.Back.";
            Add(key + "Coupled", coupler.IsCoupled());
            Add(key + "Position", coupler.transform.position);
            Add(key + "Distance", Vector3.Distance(referencePosition, coupler.transform.position));
        }
        public void Add(Bogie bogie)
        {
            Add("Bogie.Track", bogie.track?.name);
            Add("Bogie.TrackDirectionSign", bogie.TrackDirectionSign);
            Add("Bogie.Span", bogie.traveller?.Span);
            if (bogie.traveller != null)
                Add("Bogie.SpanDistance", referenceSpan - bogie.traveller?.Span);
        }
        public void Add(TrainCar car, string info)
        {
            var prefix = "Car." + info + ".";
            Add(prefix + "Info", info);
            Add(prefix + "ID", car.ID);
            Add(prefix + "IndexInTrainset", car.indexInTrainset);
            Add(prefix + "Track", car.logicCar?.CurrentTrack.ID);
            Add(prefix + "Position", car.transform.position);
            Add(prefix + "Distance", Vector3.Distance(referencePosition, car.transform.position));
            Add(car.Bogies[1]);
            Add(car.frontCoupler);
            Add(car.rearCoupler);
        }

        public void Add(Trainset set)
        {
            Add("Trainset", set.id);
            Add("Trainset.Info", $"cars {set.cars.Count} len {set.OverallLength()}");
            if (set.firstCar != null)
            {
                Add(set.firstCar, "firstCar");
            }
            if (set.lastCar != null)
            {
                Add(set.lastCar, "lastCar");
            }
            Add("", "");
        }

        internal string Generate(IOrderedEnumerable<ProximityData> allSets)
        {
            foreach (var item in allSets)
            {
                Add(item.Trainset);
            }
            return ToString();
        }
    }
}