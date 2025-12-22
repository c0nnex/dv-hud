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

        public static TrainCar CarAtEnd(TrainCar loco, bool atFront) => atFront ? FirstCar(loco) : LastCar(loco);

        public static TrainCar FirstCar(TrainCar loco) => IsFacingFrontOfTrainset(loco) ? loco.trainset.firstCar : loco.trainset.lastCar;
        public static TrainCar LastCar(TrainCar loco) => IsFacingFrontOfTrainset(loco) ? loco.trainset.lastCar : loco.trainset.firstCar;

        public static bool IsOnTrack(this Trainset trainset, Track tragetTrack) => trainset.cars.Any(c => c.logicCar?.CurrentTrack == tragetTrack);
        public static float OverallLength(this Trainset trainset) => trainset.cars.Sum(c => c.logicCar.length);
        public static float TotalMass(this Trainset trainset) => trainset.cars.Sum(c => c.massController.TotalMass);

        public static IEnumerable<TrainsetData> FilterByTrack(this IEnumerable<Trainset> allSets, Track playerTrack, bool direction, double playerSpan, Vector3 playerPosition)
        {
            foreach (var set in allSets)
            {
                if (direction) // driving forward , so we check the RearCoupler of trainset
                {
                    Coupler coupler = set.GetEndmost(false);
                    if (coupler != null)
                    {
                        var car = coupler?.train;
                        if (car?.logicCar.CurrentTrack == playerTrack)
                        {                           
                            var span = car?.Bogies[1].traveller.Span ?? 0;
                            var distSpan = span - playerSpan;
                            
                            if (distSpan >= 0) // is front of us
                                yield return new TrainsetData(car!.ID, span, distSpan < 50f ? Vector3.Distance(playerPosition, coupler!.transform.position) : distSpan, car.isStationary);
                        }
                    }
                }
                else // Driving backwards , using frontcoupler of trainset
                {
                    Coupler coupler = set.GetEndmost(true);
                    if (coupler != null)
                    {
                        var car = coupler?.train;
                        if (car?.logicCar.CurrentTrack == playerTrack)
                        {
                            var span = car?.Bogies[1].traveller.Span ?? 0;
                            var distSpan = playerSpan - span;
                            if (distSpan >= 0) // is behind of us
                                yield return new TrainsetData(car!.ID, span, distSpan < 50f ? Vector3.Distance(coupler!.transform.position, playerPosition) : distSpan, car.isStationary);
                        }
                    }
                }                
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