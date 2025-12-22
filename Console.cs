using System;
using System.Linq;
using System.Reflection;
using CommandTerminal;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.Utils;
using QuantitiesNet;
using UnityEngine;
using Random = System.Random;
using static QuantitiesNet.Units;
using DV.Simulation.Cars;
namespace DvMod.HeadsUpDisplay
{

    public static class Console
    {

#if DEBUG
        [RegisterCommand("cx.ListTrainSets", Help = "", MinArgCount = 0, MaxArgCount = 0)]
        public static void ListTrainSets(CommandArg[] args)
        {
            RailTrackRegistry? trackRegistry = UnityEngine.Object.FindObjectOfType<RailTrackRegistry>();
            var allSets = Trainset.allSets;
            Debug.Log($"Sets: {allSets.Count} Cars: {allSets.Sum(a => a.cars.Count)}");
            var playerTrack = PlayerManager.Car?.logicCar?.CurrentTrack?.ID?.FullID;
            var playerSpan = PlayerManager.Car.frontCoupler.transform.position;// FrontBogie?.traveller?.Span??0;
            var playerSet = PlayerManager.Car?.trainset?.id ?? 0; ;// allSets.FirstOrDefault(s => s.cars.Contains(PlayerManager.Car))?.id??0;
            
            foreach (var set in allSets)
            {
                if (set.id == playerSet)
                    continue;
                var trk = set.firstCar.logicCar.CurrentTrack;
                if (trk == null) continue;
                var isSameTrack = playerTrack == trk.ID.FullID;               
                if (isSameTrack)
                {
                    Debug.Log($"Set {set.id} Cars: {set.cars.Count} len {set.OverallLength()}");
                    Debug.Log($"First {set.firstCar.ID}");
                    Debug.Log($"  Front {set.firstCar.frontCoupler.IsCoupled()} {Vector3.Distance(playerSpan, set.firstCar.frontCoupler.transform.position)}m");
                    Debug.Log($"  Rear  {set.firstCar.rearCoupler.IsCoupled()} {Vector3.Distance(playerSpan, set.firstCar.rearCoupler.transform.position)}m");
                    Debug.Log($"Last {set.lastCar.ID}");
                    Debug.Log($"  Front {set.lastCar.frontCoupler.IsCoupled()} {Vector3.Distance(playerSpan, set.lastCar.frontCoupler.transform.position)}m");
                    Debug.Log($"  Rear  {set.lastCar.rearCoupler.IsCoupled()} {Vector3.Distance(playerSpan, set.lastCar.rearCoupler.transform.position)}m");
                    Debug.Log("");


/*                    set.firstCar.frontCoupler.IsCoupled();
                   
                    var spanQuantityFront = new Quantity<Dimensions.Length>(set.firstCar.FrontBogie.traveller.Span - playerSpan);
                    var spanQuantityBack = new Quantity<Dimensions.Length>(set.lastCar.RearBogie.traveller.Span - playerSpan);
                    Debug.Log($"Dist {Math.Round(spanQuantityFront.In(Meter) / 10) * 10:F0}m {Math.Round(spanQuantityBack.In(Meter) / 10) * 10:F0}m");
                   
                    Debug.Log($"{set.id} {set.firstCar.ID} => {trk.ID.ToString()} Dist {Vector3.Distance(playerSpan, set.firstCar.frontCoupler.transform.position)}m {Vector3.Distance(playerSpan, set.lastCar.rearCoupler.transform.position)}m");
*/
                }
            }
        }
#endif
        public static void RegisterCommands()
        {
            BindingFlags bindingAttr = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo[] methods = typeof(Console).GetMethods(bindingAttr);
            foreach (MethodInfo methodInfo in methods)
            {
                RegisterCommandAttribute? registerCommandAttribute;
                try
                {
                    registerCommandAttribute = Attribute.GetCustomAttribute(methodInfo, typeof(RegisterCommandAttribute)) as RegisterCommandAttribute;
                }
                catch (Exception)
                {
                    Debug.LogWarning("Could not get RegisterCommand attribute for method '" + methodInfo.Name +"'");
                    continue;
                }

                if (registerCommandAttribute == null)
                {
                        continue;
                }

                ParameterInfo[] parameters = methodInfo.GetParameters();
                                
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(CommandArg[]))
                {                   
                    continue;
                }

                Action<CommandArg[]> proc = (Action<CommandArg[]>)Delegate.CreateDelegate(typeof(Action<CommandArg[]>), methodInfo);
                Terminal.Shell.AddCommand(registerCommandAttribute.Name, proc, registerCommandAttribute.MinArgCount, registerCommandAttribute.MaxArgCount, registerCommandAttribute.Help, registerCommandAttribute.Hint, registerCommandAttribute.Secret);
            }

        }
    }
}
