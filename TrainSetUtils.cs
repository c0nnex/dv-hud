using System;
using System.Collections.Generic;
using System.Linq;
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

        public static Coupler? GetFreeCoupler(this TrainCar trainCar,Vector3 refPosition)
        {
            if (trainCar.frontCoupler.IsCoupled()) return trainCar.rearCoupler;
            if (trainCar.rearCoupler.IsCoupled()) return trainCar.frontCoupler;
            var df = Vector3.Distance(refPosition,trainCar.frontCoupler.transform.position);
            var dr = Vector3.Distance(refPosition, trainCar.rearCoupler.transform.position);
            if (df <= dr)
                return trainCar.frontCoupler;
            return trainCar.rearCoupler;
        }

        public static bool IsOnTrack(this Trainset trainset, Track tragetTrack) => trainset.cars.Any(c => c.logicCar?.CurrentTrack == tragetTrack);
        public static float OverallLength(this Trainset trainset) => trainset.cars.Sum(c => c.logicCar.length);
        public static float TotalMass(this Trainset trainset) => trainset.cars.Sum(c => c.massController.TotalMass);

        public static IEnumerable<TrainsetData> FilterByTrack(this IEnumerable<Trainset> allSets, Track playerTrack, bool direction, double playerSpan, Vector3 playerPosition)
        {
            foreach (var set in allSets)
            {
                var car = set.GetNearestCarOnTrack(playerTrack,playerPosition);
                if (car == null) continue;
                var span = car!.Bogies[1].traveller.Span;
                var distSpan = span - playerSpan;
                if (direction && distSpan < 0)
                    continue;
                if (!direction && distSpan > 0)
                    continue;
                Main.DebugLog($"Checking set {set.id} cars {set.cars.Count} first {set.firstCar.ID} last {set.lastCar.ID} selected {car!.ID} Dist {distSpan}");
                yield return new TrainsetData(car!.ID, span, distSpan < 50f ? Vector3.Distance(playerPosition, car.GetFreeCoupler(playerPosition)?.transform.position??Vector3.zero) : distSpan, car.isStationary);                
            }
        }
    }

    public class TrainsetData
    {
        public TrainsetData(string iD, double span, double distance,bool isStationary)
        {
            ID = iD;
            Span = span;
            Distance = distance;
            IsStationary = isStationary;
        }

        public string ID { get; } = string.Empty;
        public double Span { get;  } // Current relative position on Track        
        public double Distance {  get; } // distance to reference (rough or exact)
        public bool IsStationary { get; }
        public override string ToString()
        {
            var str = "[" + ID + "]";
            if (IsStationary)
                str = "[<color=red>"+ID+"</color>]";
            var color = "white";
            if (Distance < 50f)
                color = Distance < 10f ? "red" : "orange" ;
            else if (Distance < 200f)
                color = "yellow";          
            if (Distance > 50f) 
                return $"{str} <color={color}>{Distance:F0} m</color>";
            return $"{str} <color={color}>{Distance:F2} m</color>";
        }
    }
}