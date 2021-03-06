﻿using Aimtec;
using System.Linq;
using System.Drawing;
using Aimtec.SDK.Menu;
using Aimtec.SDK.Util;
using Aimtec.SDK.Damage;
using Aimtec.SDK.Orbwalking;
using Aimtec.SDK.Extensions;
using Spell = Aimtec.SDK.Spell;
using Aimtec.SDK.TargetSelector;
using Aimtec.SDK.Menu.Components;
using System.Collections.Generic;
using Aimtec.SDK.Util.ThirdParty;
using Aimtec.SDK.Prediction.Skillshots;

namespace FrOnDaL_Varus
{
    internal class FrOnDaLVarus
    {
        public static Menu Main = new Menu("Index", "FrOnDaL Varus", true);
        public static Orbwalker Orbwalker = new Orbwalker();
        public static Obj_AI_Hero Varus => ObjectManager.GetLocalPlayer();
        private static Spell _q, _e, _r;
        internal static bool IsPreAa;
        internal static bool IsAfterAa;    
        public static double QDamage(Obj_AI_Base d)
        {
            var damageQ = Varus.CalculateDamage(d, DamageType.Physical, (float)new double[] { 12, 58, 104, 150, 196 }[Varus.SpellBook.GetSpell(SpellSlot.Q).Level - 1] + Varus.TotalAttackDamage / 100 * 132);
            return (float)damageQ;
        }     
        public static readonly List<string> SpecialChampions = new List<string> { "Annie", "Jhin" };
        public static int SxOffset(Obj_AI_Hero target)
        {
            return SpecialChampions.Contains(target.ChampionName) ? 1 : 10;
        }
        public static int SyOffset(Obj_AI_Hero target)
        {
            return SpecialChampions.Contains(target.ChampionName) ? 7 : 15;
        }
        public static int GetBuffCount(Obj_AI_Base target) => target.GetBuffCount("VarusWDebuff");

        public FrOnDaLVarus()
        {
            /*Spells*/
            _q = new Spell(SpellSlot.Q, 1000);
            _e = new Spell(SpellSlot.E, 925);
            _r = new Spell(SpellSlot.R, 1200f);

            _q.SetSkillshot(0.25f, 70, 1900, false, SkillshotType.Line);
            _q.SetCharged("VarusQ", "VarusQ", 1000, 1600, 1.3f);           
            _e.SetSkillshot(250, 235, 1500f, false, SkillshotType.Circle);
            _r.SetSkillshot(250f, 120f, 1950f, false, SkillshotType.Line);

            Orbwalker.Attach(Main);

            /*Combo Menu*/
            var combo = new Menu("combo", "Combo");
            {
                combo.Add(new MenuBool("q", "Use Combo Q"));
                combo.Add(new MenuSliderBool("qstcW", "Minimum W stack for Q", false, 2, 1, 3));
                var whiteListQ = new Menu("whiteListQ", "Q White List");
                {
                    foreach (var enemies in GameObjects.EnemyHeroes)
                    {
                        whiteListQ.Add(new MenuBool("qWhiteList" + enemies.ChampionName.ToLower(), enemies.ChampionName));
                    }
                }              
                combo.Add(whiteListQ);
                combo.Add(new MenuBool("e", "Use Combo E"));
                combo.Add(new MenuSlider("UnitsEhit", "E Hit x Units Enemy", 1, 1, 3));
                combo.Add(new MenuSliderBool("eStcW", "Minimum W stack for E", false, 1, 1, 3));
                combo.Add(new MenuKeyBind("keyR", "R Key:", KeyCode.T, KeybindType.Press));              
                combo.Add(new MenuSlider("rHit", "Minimum enemies for R", 1, 1, 5));
                combo.Add(new MenuSliderBool("autoR", "Auto R Minimum enemies for", false, 3, 1, 5));
                var whiteListR = new Menu("whiteListR", "R White List");
                {
                    foreach (var enemies in GameObjects.EnemyHeroes)
                    {
                        whiteListR.Add(new MenuBool("rWhiteList" + enemies.ChampionName.ToLower(), enemies.ChampionName));
                    }
                }
                combo.Add(whiteListR);
            }                      
            Main.Add(combo);

            /*Harass Menu*/
            var harass = new Menu("harass", "Harass");
            {
                harass.Add(new MenuBool("autoHarass", "Auto Harass", false));
                harass.Add(new MenuKeyBind("keyHarass", "Harass Key:", KeyCode.C, KeybindType.Press));
                harass.Add(new MenuSliderBool("q", "Use Q / if Mana >= x%", true, 30, 0, 99));
                var whiteListQ = new Menu("whiteListQ", "Q White List");
                {
                    foreach (var enemies in GameObjects.EnemyHeroes)
                    {
                        whiteListQ.Add(new MenuBool("qWhiteList" + enemies.ChampionName.ToLower(), enemies.ChampionName));
                    }
                }
                harass.Add(whiteListQ);
                harass.Add(new MenuSliderBool("e", "Use E / if Mana >= x%", false, 30, 0, 99));
            }
            Main.Add(harass);

            /*LaneClear Menu*/
            var laneclear = new Menu("laneclear", "Lane Clear")
            {
                new MenuSliderBool("q", "Use Q / if Mana >= x%", true, 60, 0, 99),
                new MenuSlider("UnitsQhit", "Q Hit x Units minions >= x%", 3, 1, 6),
                new MenuSliderBool("e", "Use E / if Mana >= x%", false, 60, 0, 99),
                new MenuSlider("UnitsEhit", "E Hit x Units minions >= x%", 3, 1, 4)
            };
            Main.Add(laneclear);

            /*JungleClear Menu*/
            var jungleclear = new Menu("jungleclear", "Jungle Clear")
            {
                new MenuSliderBool("q", "Use Q / if Mana >= x%", true, 30, 0, 99),
                new MenuSliderBool("jungW", "Minimum W stack for Q", false, 2, 1, 3),
                new MenuSliderBool("e", "Use E / if Mana >= x%", true, 30, 0, 99)
            };
            Main.Add(jungleclear);

            /*Drawings Menu*/
            var drawings = new Menu("drawings", "Drawings")
            {
                new MenuBool("q", "Draw Q"),
                new MenuBool("e", "Draw E", false),
                new MenuBool("r", "Draw R", false),
                new MenuBool("drawDamage", "Use Draw Q Damage")
            };
            Main.Add(drawings);
            Main.Attach();

            Game.OnUpdate += Game_OnUpdate;
            Render.OnPresent += DamageDraw;
            Render.OnPresent += SpellDraw;
            Orbwalker.PreAttack += OnPreAttack;
            Orbwalker.PreAttack += (a, b) => IsPreAa = true;
            Orbwalker.PostAttack += (a, b) => { IsPreAa = false; IsAfterAa = true; };

        }

        /*Drawings*/
        private static void SpellDraw()
        {
            if (Main["drawings"]["q"].As<MenuBool>().Enabled)
            {
                Render.Circle(Varus.Position, _q.ChargedMinRange, 180, Color.Green);
            }
            if (Main["drawings"]["e"].As<MenuBool>().Enabled)
            {
                Render.Circle(Varus.Position, _e.Range, 180, Color.Green);
            }
            if (Main["drawings"]["r"].As<MenuBool>().Enabled)
            {
                Render.Circle(Varus.Position, _r.Range, 180, Color.Green);
            }
        }

        private static void Game_OnUpdate()
        {
            if (Varus.IsDead || MenuGUI.IsChatOpen()) return;
            switch (Orbwalker.Mode)
            {
                case OrbwalkingMode.Combo:
                    Combo();
                    break;
                case OrbwalkingMode.Laneclear:
                    LaneClear();
                    JungleClear();
                    break;
                case OrbwalkingMode.Mixed:
                    Harass();
                    break;
            }

            if (Main["harass"]["autoHarass"].As<MenuBool>().Enabled && Orbwalker.Mode != OrbwalkingMode.Laneclear && Orbwalker.Mode != OrbwalkingMode.Combo)
            {
                Harass();
            }

            if (Main["combo"]["keyR"].As<MenuKeyBind>().Enabled)
            {
                ManualR();
            }
            if (Main["combo"]["autoR"].As<MenuSliderBool>().Enabled)
            {
                var targetR = TargetSelector.GetTarget(_r.Range - 100);
                if (targetR == null) return;
                var rHit = GameObjects.EnemyHeroes.Where(x => x.Distance(targetR) <= 450f).ToList();
                if (Main["combo"]["whiteListR"]["rWhiteList" + targetR.ChampionName.ToLower()].As<MenuBool>().Enabled && _r.Ready &&
                    targetR.IsInRange(_r.Range - 100) && targetR.IsValidTarget(_r.Range - 100) && rHit.Count >= Main["combo"]["autoR"].As<MenuSliderBool>().Value)
                {
                    _r.Cast(targetR.Position);
                }
            }
        }

        /*Comob*/
        private static void Combo()
        {
            var targetC = TargetSelector.GetTarget(_q.ChargedMaxRange - 100);
            if (targetC == null) return;
            if (Main["combo"]["q"].As<MenuBool>().Enabled && Main["combo"]["whiteListQ"]["qWhiteList" + targetC.ChampionName.ToLower()].As<MenuBool>().Enabled && _q.Ready)
            {
                if ((Main["combo"]["qstcW"].As<MenuSliderBool>().Enabled && Varus.Distance(targetC) < 750 && GetBuffCount(targetC) >= Main["combo"]["qstcW"].As<MenuSliderBool>().Value) || !Main["combo"]["qstcW"].As<MenuSliderBool>().Enabled || _q.ChargePercent >= 100 || targetC.Health <= QDamage(targetC) || Varus.Distance(targetC) > 800)
                {                                             
                if (!_q.IsCharging && !IsPreAa)
                {
                    _q.StartCharging(_q.GetPrediction(targetC).CastPosition); return;
                }
                if (!_q.IsCharging) return;              
                    if (Varus.Distance(targetC) < 750 && _q.ChargePercent >= 30 || Varus.Distance(targetC) > 750 && _q.ChargePercent >= 100)
                    {
                        var prediction = _q.GetPrediction(targetC);

                        if (prediction.HitChance >= HitChance.Medium)
                        {
                            _q.Cast(_q.GetPrediction(targetC).CastPosition);
                        }                     
                    }
               }
        }

            if (!Main["combo"]["e"].As<MenuBool>().Enabled || !targetC.IsValidTarget(_e.Range) || !_e.Ready) return;

            if ((!Main["combo"]["eStcW"].As<MenuSliderBool>().Enabled || !(Varus.Distance(targetC) < 700) ||
                 GetBuffCount(targetC) < Main["combo"]["eStcW"].As<MenuSliderBool>().Value) && Main["combo"]["eStcW"].As<MenuSliderBool>().Enabled && !(Varus.Distance(targetC) > 750)) return;
            foreach (var enemy in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(_e.Range)))
            {
                if (enemy == null) continue;
                if (GameObjects.EnemyHeroes.Count(t => t.IsValidTarget(_e.Width, false, true, _e.GetPrediction(enemy).CastPosition)) >= Main["combo"]["UnitsEhit"].As<MenuSlider>().Value)
                {
                    _e.Cast(targetC.Position);
                }
            }
        }

        /*Harass*/
        private static void Harass()
        {
            var targetH = TargetSelector.GetTarget(_q.ChargedMaxRange - 100);
            if (targetH == null) return;
            if (Main["harass"]["q"].As<MenuSliderBool>().Enabled && Main["harass"]["whiteListQ"]["qWhiteList" + targetH.ChampionName.ToLower()].As<MenuBool>().Enabled && _q.Ready && !Varus.IsUnderEnemyTurret())
            {
                if (!_q.IsCharging && !IsPreAa && Varus.ManaPercent() > Main["harass"]["q"].As<MenuSliderBool>().Value)
                {
                    _q.StartCharging(_q.GetPrediction(targetH).CastPosition); return;
                }
                if (!_q.IsCharging) return;
                if (Varus.Distance(targetH) < 750 && _q.ChargePercent >= 30 || Varus.Distance(targetH) > 750 && _q.ChargePercent >= 100)
                {
                    _q.Cast(_q.GetPrediction(targetH).CastPosition);
                }           
            }

            if (!Main["harass"]["e"].As<MenuSliderBool>().Enabled || !(Varus.ManaPercent() > Main["harass"]["e"].As<MenuSliderBool>().Value) || Varus.IsUnderEnemyTurret() ||
                !targetH.IsValidTarget(_e.Range) || !_e.Ready) return;
            foreach (var enemy in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(_e.Range)))
            {
                if (enemy == null) continue;
                if (GameObjects.EnemyHeroes.Count(t => t.IsValidTarget(_e.Width, false, false, _e.GetPrediction(enemy).CastPosition)) >= 1)
                {
                    _e.Cast(enemy.Position);
                }
            }
        }
    
        /*Lane Clear*/
        private static void LaneClear()
        {            
            if (Main["laneclear"]["q"].As<MenuSliderBool>().Enabled && _q.Ready)
            {
                foreach (var targetL in GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_q.ChargedMaxRange)))
                {
                    var range = _q.IsCharging ? _q.Range : _q.ChargedMaxRange;
                    var result = GetLinearLocation(range, _q.Width);
                    if (result == null) continue;
                    if (Varus.ManaPercent() >= Main["laneclear"]["q"].As<MenuSliderBool>().Value &&
                        result.NumberOfMinionsHit >= Main["laneclear"]["UnitsQhit"].As<MenuSlider>().Value &&
                        !Varus.IsUnderEnemyTurret() && !_q.IsCharging && !IsPreAa)
                    {
                        _q.StartCharging(result.CastPosition);
                        return;
                    }
                    if (!_q.IsCharging) return;
                    if (Varus.Distance(targetL) > 600 && _q.ChargePercent >= 90)
                    {
                        _q.Cast(result.CastPosition);
                    }
                    else if (Varus.Distance(targetL) < 600 && _q.ChargePercent >= 65)
                    {
                        _q.Cast(result.CastPosition);
                    }
                }
            }

            if (!Main["laneclear"]["e"].As<MenuSliderBool>().Enabled || !(Varus.ManaPercent() > Main["laneclear"]["e"].As<MenuSliderBool>().Value) || !_e.Ready) return;
            {
                foreach (var targetE in GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_e.Range)))
                {
                    if (targetE == null) continue;
                    if (GameObjects.EnemyMinions.Count(t => t.IsValidTarget(_e.Width, false, false, _e.GetPrediction(targetE).CastPosition)) >= Main["laneclear"]["UnitsEhit"].As<MenuSlider>().Value && !Varus.IsUnderEnemyTurret())
                    {
                        _e.Cast(_e.GetPrediction(targetE).CastPosition);
                    }
                }
            }
        }  

        /*Jungle Clear*/
        private static void JungleClear()
        {
            foreach (var targetJ in GameObjects.Jungle.Where(x => !GameObjects.JungleSmall.Contains(x) && (GameObjects.JungleLarge.Contains(x) || GameObjects.JungleLegendary.Contains(x)) && x.IsValidTarget(_q.ChargedMaxRange)))
            {
                if (Main["jungleclear"]["q"].As<MenuSliderBool>().Enabled && targetJ.IsValidTarget(1000) && _q.Ready)
                {
                    if (Main["jungleclear"]["jungW"].As<MenuSliderBool>().Enabled && GetBuffCount(targetJ) >=
                        Main["jungleclear"]["jungW"].As<MenuSliderBool>().Value || !Main["jungleclear"]["jungW"].As<MenuSliderBool>().Enabled || _q.ChargePercent >= 100)
                    {
                    
                    if (!_q.IsCharging && Varus.ManaPercent() >= Main["jungleclear"]["q"].As<MenuSliderBool>().Value)
                    {
                        if (!IsPreAa)
                            _q.StartCharging(_q.GetPrediction(targetJ).CastPosition);
                    }
                    else if (_q.IsCharging && _q.ChargePercent >= 100)
                    {
                        _q.Cast(targetJ.Position);
                    }  
                    else if (Varus.Distance(targetJ) < 700 && _q.ChargePercent >= 30)
                    {
                        _q.Cast(targetJ.Position);
                    }
                    }
                }               

                if (Main["jungleclear"]["e"].As<MenuSliderBool>().Enabled && Varus.ManaPercent() > Main["jungleclear"]["e"].As<MenuSliderBool>().Value && targetJ.IsValidTarget(_e.Range) && _e.Ready)
                {
                    _e.Cast(targetJ.Position);
                }
            }          
        }

        private static void ManualR()
        {
            var targetR = TargetSelector.GetTarget(_r.Range - 100);
            if (targetR == null) return;
            var rHit = GameObjects.EnemyHeroes.Where(x => x.Distance(targetR) <= 450f).ToList();
            if (Main["combo"]["whiteListR"]["rWhiteList" + targetR.ChampionName.ToLower()].As<MenuBool>().Enabled && _r.Ready &&
                targetR.IsInRange(_r.Range - 100) && targetR.IsValidTarget(_r.Range - 100) && rHit.Count >= Main["combo"]["rHit"].As<MenuSlider>().Value)
            {              
                    _r.Cast(targetR.Position);                                         
            }
        }

        /*W Stacks Damage*/
        private static float StacksWDamage(Obj_AI_Base unit)
        {
            if (GetBuffCount(unit) == 0) return 0;
            float[] damageStackW = { 0, 0.02f, 0.0275f, 0.035f, 0.0425f, 0.05f };
            var stacksWCount = GetBuffCount(unit);
            var extraDamage = 2 * (Varus.FlatMagicDamageMod / 100);
            var damageW = unit.MaxHealth * damageStackW[Varus.SpellBook.GetSpell(SpellSlot.W).Level] * stacksWCount + (extraDamage - extraDamage % 2);
            var expiryDamage = Varus.CalculateDamage(unit, DamageType.Magical, damageW > 360 && unit.GetType() != typeof(Obj_AI_Hero) ? 360 : damageW);
            return (float)expiryDamage;
        }

        /*Draw Damage Q*/
        private static void DamageDraw()
        {
            if (!Main["drawings"]["drawDamage"].Enabled || Varus.SpellBook.GetSpell(SpellSlot.Q).Level <= 0) return;
            foreach (var enemy in GameObjects.EnemyHeroes.Where(x => !x.IsDead && Varus.Distance(x) < 1700))
            {                                    
                const int width = 103;
                var xOffset = SxOffset(enemy);
                var yOffset = SyOffset(enemy);
                var barPos = enemy.FloatingHealthBarPosition;
                barPos.X += xOffset;
                barPos.Y += yOffset;
                var drawEndXPos = barPos.X + width * (enemy.HealthPercent() / 100);
                var drawStartXPos = (float)(barPos.X + (enemy.Health > QDamage(enemy) + StacksWDamage(enemy) ? width * ((enemy.Health - (QDamage(enemy) + StacksWDamage(enemy))) / enemy.MaxHealth * 100 / 100) : 0));
                Render.Line(drawStartXPos, barPos.Y, drawEndXPos, barPos.Y, 9, true, enemy.Health < QDamage(enemy) + StacksWDamage(enemy) ? Color.GreenYellow : Color.ForestGreen);                   
            }
        }

        public static void OnPreAttack(object sender, PreAttackEventArgs args)
        {
            switch (Orbwalker.Mode)
            {

                case OrbwalkingMode.Combo:
                case OrbwalkingMode.Mixed:
                case OrbwalkingMode.Lasthit:
                case OrbwalkingMode.Laneclear:
                    if (Varus.HasBuff("VarusQ") && _q.IsCharging)
                    {
                        args.Cancel = true;
                    }
                    break;
            }
        }

        /*Polygon*/
        public abstract class Polygon
        {
            public List<Vector3> Points = new List<Vector3>();

            public List<IntPoint> ClipperPoints
            {
                get
                {
                    return Points.Select(p => new IntPoint(p.X, p.Z)).ToList();
                }
            }

            public bool Contains(Vector3 point)
            {
                var p = new IntPoint(point.X, point.Z);
                var inpolygon = Clipper.PointInPolygon(p, ClipperPoints);
                return inpolygon == 1;
            }
        }
        public class Rectangle : Polygon
        {
            public Rectangle(Vector3 startPosition, Vector3 endPosition, float width)
            {
                var direction = (startPosition - endPosition).Normalized();
                var perpendicular = Perpendicular(direction);

                var leftBottom = startPosition + width * perpendicular;
                var leftTop = startPosition - width * perpendicular;

                var rightBottom = endPosition - width * perpendicular;
                var rightLeft = endPosition + width * perpendicular;

                Points.Add(leftBottom);
                Points.Add(leftTop);
                Points.Add(rightBottom);
                Points.Add(rightLeft);
            }

            public Vector3 Perpendicular(Vector3 v)
            {
                return new Vector3(-v.Z, v.Y, v.X);
            }
        }

        /*Q Minions Location*/
        public class LaneclearResult
        {
            public LaneclearResult(int hit, Vector3 cp)
            {
                NumberOfMinionsHit = hit;
                CastPosition = cp;
            }

            public int NumberOfMinionsHit;
            public Vector3 CastPosition;
        }

        public static LaneclearResult GetLinearLocation(float range, float width)
        {
            var minions = ObjectManager.Get<Obj_AI_Base>().Where(x => x.IsValidSpellTarget(range));

            var objAiBases = minions as Obj_AI_Base[] ?? minions.ToArray();
            var positions = objAiBases.Select(x => x.ServerPosition).ToList();

            var locations = new List<Vector3>();

            locations.AddRange(positions);

            var max = positions.Count;

            for (var i = 0; i < max; i++)
            {
                for (var j = 0; j < max; j++)
                {
                    if (positions[j] != positions[i])
                    {
                        locations.Add((positions[j] + positions[i]) / 2);
                    }
                }
            }

            var results = new HashSet<LaneclearResult>();

            foreach (var p in locations)
            {
                var rect = new Rectangle(Varus.Position, p, width);

                var count = objAiBases.Count(m => rect.Contains(m.Position));

                results.Add(new LaneclearResult(count, p));
            }

            var maxhit = results.MaxBy(x => x.NumberOfMinionsHit);

            return maxhit;
        }
    }
}
