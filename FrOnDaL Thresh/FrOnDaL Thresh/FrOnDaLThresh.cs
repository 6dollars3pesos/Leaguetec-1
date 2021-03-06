﻿using Aimtec;
using System.Linq;
using System.Drawing;
using Aimtec.SDK.Menu;
using Aimtec.SDK.Util;
using Aimtec.SDK.Orbwalking;
using Aimtec.SDK.Extensions;
using Spell = Aimtec.SDK.Spell;
using Aimtec.SDK.TargetSelector;
using Aimtec.SDK.Menu.Components;
using Aimtec.SDK.Prediction.Skillshots;

namespace FrOnDaL_Thresh
{
    internal class FrOnDaLThresh
    {
        public static Menu Main = new Menu("Index", "FrOnDaL Thresh", true);
        public static Orbwalker Orbwalker = new Orbwalker();
        public static Obj_AI_Hero Thresh => ObjectManager.GetLocalPlayer();
        private static Spell _q, _w, _e, _r;
        public static bool IsQActive => Thresh.SpellBook.GetSpell(SpellSlot.Q).SpellData.Name == "threshQ";
        public FrOnDaLThresh()
        {
            /*Spells*/
            _q = new Spell(SpellSlot.Q, 1070);
            _w = new Spell(SpellSlot.W, 950);
            _e = new Spell(SpellSlot.E, 500);
            _r = new Spell(SpellSlot.R, 450);
            _q.SetSkillshot(0.5f, 70, 1900f, true, SkillshotType.Line);
            _w.SetSkillshot(0.5f, 50f, 2200f, false, SkillshotType.Circle); 


            Orbwalker.Attach(Main);

            /*Combo Menu*/
            var combo = new Menu("combo", "Combo")
            {
                new MenuBool("q", "Use Combo Q"),
                new MenuBool("q2Turret", "Use Q2 Under Enemy Turret (On/Off)", false),
                new MenuBool("q2", "Use Combo Q2 (On/Off)"),
                new MenuSlider("QMinimumRange", "Q minimum range to cast", 400, 100, 600),
                new MenuSlider("QMaximumRange", "Q Maximum range to cast", 1070, 600, 1070),
                new MenuBool("w", "Use Combo W"),
                new MenuBool("wAlly", "AA range in enemy, use Ally W", false),
                //new MenuBool("wJung", "Use W To Ally Jungler"),
                new MenuBool("e", "Use Combo E"),
                new MenuList("ePP", "E Push/Pull", new []{ "Push", "Pull" }, 1),
                new MenuKeyBind("keyFlee", "E Flee Key:", KeyCode.X, KeybindType.Press),
                new MenuSliderBool("r", "Use Combo R - Minimum enemies for R",true, 3, 1, 5),
                new MenuKeyBind("keyR", "Utli Key:", KeyCode.T, KeybindType.Press),
                new MenuBool("disableAA", "Disable AutoAttacks", false)
            };
            var whiteList = new Menu("whiteList", "Q White List");
            {
                foreach (var enemies in GameObjects.EnemyHeroes)
                {
                    whiteList.Add(new MenuBool("qWhiteList" + enemies.ChampionName.ToLower(), enemies.ChampionName));
                }
            }              
            Main.Add(whiteList);
            Main.Add(combo);

            /*Harass Menu*/
            var autoW = new Menu("autoW", "Auto W Protect");
            {
                autoW.Add(new MenuSliderBool("wAuto", "Use Auto W / if Mana >= x%", true, 60, 0, 99));
                foreach (var ally in GameObjects.AllyHeroes)
                {
                    autoW.Add(new MenuSliderBool("allyW" + ally.ChampionName.ToLower(), ally.ChampionName, false, 15, 0, 99));
                }
            }
            Main.Add(autoW);

            /*Harass Menu*/
            var harass = new Menu("harass", "Harass")
            {
                new MenuBool("autoHarass", "Auto Harass", false),
                new MenuSliderBool("q", "Auto Use Q / if Mana >= x%", true, 70, 0, 99),
                new MenuSliderBool("e", "Auto Use E / if Mana >= x%", false, 70, 0, 99),
                new MenuKeyBind("keyHarass", "Harass Key:", KeyCode.C, KeybindType.Press),
                new MenuBool("EqHarass", "E after Use Q (E and Q If ready)")
            };
            Main.Add(harass);

            /*Drawings Menu*/
            var drawings = new Menu("drawings", "Drawings")
            {
                new MenuBool("q", "Draw Q"),
                new MenuBool("w", "Draw W", false),
                new MenuBool("e", "Draw E", false),
                new MenuBool("r", "Draw R", false)
            };
            Main.Add(drawings);
            Main.Attach();

            Game.OnUpdate += Game_OnUpdate;           
            Render.OnPresent += SpellDraw;
            Orbwalker.PreAttack += OnPreAttack;
        }

        /*Drawings*/
        private static void SpellDraw()
        {
            if (Main["drawings"]["q"].As<MenuBool>().Enabled)
            {
                Render.Circle(Thresh.Position, _q.Range, 180, Color.Green);
            }
            if (Main["drawings"]["w"].As<MenuBool>().Enabled)
            {
                Render.Circle(Thresh.Position, _w.Range, 180, Color.Green);
            }
            if (Main["drawings"]["e"].As<MenuBool>().Enabled)
            {
                Render.Circle(Thresh.Position, _e.Range, 180, Color.Green);
            }
            if (Main["drawings"]["r"].As<MenuBool>().Enabled)
            {
                Render.Circle(Thresh.Position, _r.Range, 180, Color.Green);
            }
        }

        private static void Game_OnUpdate()
        {
            if (Thresh.IsDead || MenuGUI.IsChatOpen()) return;
            switch (Orbwalker.Mode)
            {
                case OrbwalkingMode.Combo:
                    Combo();
                    break;
                case OrbwalkingMode.Mixed:
                    ThreshEq();
                    break;
                case OrbwalkingMode.Lasthit:
                    ThreshEflee();
                    break;
            }
            if (Main["harass"]["autoHarass"].As<MenuBool>().Enabled && Orbwalker.Mode != OrbwalkingMode.Combo && Orbwalker.Mode != OrbwalkingMode.Mixed)
            {
                ThreshQ();
                ThreshE();
            }
            if (Main["autoW"]["wAuto"].As<MenuSliderBool>().Enabled && Thresh.ManaPercent() > Main["autoW"]["wAuto"].As<MenuSliderBool>().Value && _w.Ready)
            {
                ThreshAutoW();
            }
            if (Main["combo"]["keyR"].As<MenuKeyBind>().Enabled)
            {
                if (Thresh.CountEnemyHeroesInRange(_r.Range - 50) >= 1)
                {
                    _r.Cast();
                }
            }
        }
        /*Combo*/
        private static void Combo()
        {
            ThreshQ();
            ThreshW();
            ThreshE();
            ThreshR();
        }

        private static void ThreshQ()
        {
            var target = TargetSelector.GetTarget(Main["combo"]["QMaximumRange"].As<MenuSlider>().Value);

            if (target == null) return;           
            var prediction = _q.GetPrediction(target);

            if (Main["combo"]["q"].As<MenuBool>().Enabled && Main["whiteList"]["qWhiteList" + target.ChampionName.ToLower()].As<MenuBool>().Enabled && target.IsInRange(_q.Range) && target.IsValidTarget() && !target.HasBuff("threshQ") && _q.Ready && target.Distance(Thresh) > Main["combo"]["QMinimumRange"].As<MenuSlider>().Value)
            {
                if (prediction.HitChance >= HitChance.High)
                {
                    _q.Cast(prediction.CastPosition);                                                   
                }                
            }
            if (target.HasBuff("threshQ") && Main["combo"]["q2"].As<MenuBool>().Enabled && !target.IsUnderEnemyTurret())
            {
                if (target.Distance(Thresh.ServerPosition) >= 400)
                {
                    DelayAction.Queue(1000, () => _q.CastOnUnit(Thresh));
                }
            }
            if (target.HasBuff("threshQ") && target.IsUnderEnemyTurret() && Main["combo"]["q2Turret"].As<MenuBool>().Enabled)
            {
                if (target.Distance(Thresh.ServerPosition) >= 400)
                {
                    DelayAction.Queue(1000, () => _q.CastOnUnit(Thresh));
                }            
            }           
        }

        private static void ThreshW()
        {
            var ally = GameObjects.AllyHeroes.Where(x => x.IsInRange(_q.Range + 400) && !x.IsDead && x.IsAlly && !x.IsMe).FirstOrDefault(x => x.Distance(Thresh.Position) <= 1400);
            var target = TargetSelector.GetTarget(_q.Range);
            if (target == null) return;
            
            if (Main["combo"]["w"].As<MenuBool>().Enabled && ally != null && _w.Ready)
            {
                if (ally.Distance(Thresh.ServerPosition) <= 700) return;
                if (target.HasBuff("threshQ"))
                {       
                    _w.Cast(ally.ServerPosition);
                }
                if (target.Distance(Thresh) <= 400 && Main["combo"]["wAlly"].As<MenuBool>().Enabled)
                {
                    _w.Cast(ally.ServerPosition);
                }
            }
        }

        private static void ThreshAutoW()
        {
            foreach (var ally in GameObjects.AllyHeroes.Where(x => x.IsInRange(_w.Range) && !x.IsDead && x.IsAlly && x.HealthPercent() <= Main["autoW"]["allyW" + x.ChampionName.ToLower()].Value && x.CountEnemyHeroesInRange(700) >= 1))
            {
                if (ally.IsInRange(_w.Range) && !Thresh.IsRecalling() && Main["autoW"]["allyW" + ally.ChampionName.ToLower()].As<MenuSliderBool>().Enabled)
                {
                    _w.CastOnUnit(ally);
                }
            }
        }

        private static void ThreshE()
        {         
            var target = TargetSelector.GetTarget(_e.Range);
            if (target == null) return;              
            switch (Main["combo"]["ePP"].As<MenuList>().Value)
            {
                case 0:
                    if (Main["combo"]["e"].As<MenuBool>().Enabled && target.IsInRange(_e.Range) && !IsQActive && _e.Ready)
                    {
                        _e.Cast(target.Position);
                    }
                    break;
                case 1:
                    if (Main["combo"]["e"].As<MenuBool>().Enabled && target.IsInRange(_e.Range) && !IsQActive && _e.Ready)
                    {                                             
                      _e.Cast(target.Position.Extend(Thresh.ServerPosition, Vector3.Distance(target.Position, Thresh.Position) + 400));   
                    }              
                    break;
            }
        }

        private static void ThreshEq()
        {
            if (Main["harass"]["EqHarass"].As<MenuBool>().Enabled)
            {      
            var target = TargetSelector.GetTarget(Main["combo"]["QMaximumRange"].As<MenuSlider>().Value);
            if (target == null) return;
            if (target.IsInRange(_e.Range) && !target.HasBuff("threshQ") && _e.Ready)
            {
                _e.Cast(target.Position.Extend(Thresh.ServerPosition, Vector3.Distance(target.Position, Thresh.Position) + 400));
            }
            var prediction = _q.GetPrediction(target);

            if (Main["whiteList"]["qWhiteList" + target.ChampionName.ToLower()].As<MenuBool>().Enabled && target.IsInRange(_q.Range) && target.IsValidTarget() && !target.HasBuff("threshQ") && _q.Ready && target.Distance(Thresh) > _e.Range && !_e.Ready)
            {
                if (prediction.HitChance >= HitChance.High)
                {
                    _q.Cast(prediction.CastPosition);
                }
            }
            if (target.HasBuff("threshQ") && Main["combo"]["q2"].As<MenuBool>().Enabled && !target.IsUnderEnemyTurret())
            {
                if (target.Distance(Thresh.ServerPosition) >= 400)
                {
                    DelayAction.Queue(1000, () => _q.CastOnUnit(Thresh));
                }
            }
            if (target.HasBuff("threshQ") && target.IsUnderEnemyTurret() && Main["combo"]["q2Turret"].As<MenuBool>().Enabled)
            {
                if (target.Distance(Thresh.ServerPosition) >= 400)
                {
                    DelayAction.Queue(1000, () => _q.CastOnUnit(Thresh));
                }
            }

            var ally = GameObjects.AllyHeroes.Where(x => x.IsInRange(_q.Range + 400) && !x.IsDead && x.IsAlly && !x.IsMe).FirstOrDefault(x => x.Distance(Thresh.Position) <= 1400);     

            if (ally != null && _w.Ready)
            {
                if (ally.Distance(Thresh.ServerPosition) <= 700) return;
                if (target.HasBuff("threshQ"))
                {
                    _w.Cast(ally.ServerPosition);
                }
                if (target.Distance(Thresh) <= 400 && Main["combo"]["wAlly"].As<MenuBool>().Enabled)
                {
                    _w.Cast(ally.ServerPosition);
                }
            }
            }
        }

        private static void ThreshEflee()
        {
            var target = TargetSelector.GetTarget(_e.Range);
            if (target == null) return;
          
                    if (Main["combo"]["keyFlee"].As<MenuKeyBind>().Enabled && target.IsInRange(_e.Range) && _e.Ready)
                    {
                        _e.Cast(target.Position);
                    }       
        }

        private static void ThreshR()
        {
            if (Main["combo"]["r"].As<MenuSliderBool>().Enabled && Thresh.CountEnemyHeroesInRange(_r.Range - 50) >= Main["combo"]["r"].As<MenuSliderBool>().Value)
            {
                _r.Cast();
            }
        }
        public static void OnPreAttack(object sender, PreAttackEventArgs args)
        {
            switch (Orbwalker.Mode)
            {
          
                case OrbwalkingMode.Mixed:
                    if (!_e.Ready) return;                    
                        args.Cancel = true;                                      
                    break;   
                              
                case OrbwalkingMode.Combo:
                case OrbwalkingMode.Lasthit:
                case OrbwalkingMode.Laneclear:
                    if (GameObjects.EnemyMinions.Contains(args.Target) && Main["combo"]["disableAA"].As<MenuBool>().Enabled)
                    {
                        args.Cancel = GameObjects.AllyHeroes.Any(a => !a.IsMe && a.Distance(Thresh) < 2500);
                    }
                    break;
            }
        }
    }
}
