﻿using NBitcoin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace ZBSolutions
{
    public class ZerionWallet : Wlt
    {
        protected readonly string _extId;
        protected readonly string _fileName;

        public ZerionWallet(IZennoPosterProjectModel project, Instance instance, bool log = false, string key = null, string seed = null)
            : base(project, instance, log)
        {
            _extId = "klghhnkeealcohjjanjjdaeeggmfmlpl";
            _fileName = "Zerion1.21.3.crx";
            _key = KeyCheck(key);
            _seed = SeedCheck(seed);
        }

        private string KeyCheck(string key)
        {
            if (string.IsNullOrEmpty(key))
                key = Decrypt(KeyT.secp256k1);
            if (string.IsNullOrEmpty(key))
                throw new Exception("emptykey");
            return key;
        }
        private string SeedCheck(string seed)
        {
            if (string.IsNullOrEmpty(seed))
                seed = Decrypt(KeyT.bip39);
            if (string.IsNullOrEmpty(seed))
                throw new Exception("emptykey");
            return seed;
        }

        public void ZerionLnch(string fileName = null, bool log = false)
        {
            if (string.IsNullOrEmpty(fileName)) fileName = _fileName;

            var em = _instance.UseFullMouseEmulation;
            _instance.UseFullMouseEmulation = false;

            if (Install(_extId, fileName)) ZerionImport(log: log);
            else
            {
                ZerionUnlock(log: false);
                ZerionCheck(log: log);
            }
            _instance.CloseExtraTabs();
            _instance.UseFullMouseEmulation = em;
        }

        public bool ZerionImport(string source = "pkey", string refCode = null, bool log = false)
        {
            if (string.IsNullOrWhiteSpace(refCode))
            {

                refCode = _sql.DbQ(@"SELECT referralCode
                FROM projects.zerion
                WHERE referralCode != '_' 
                AND TRIM(referralCode) != ''
                ORDER BY RANDOM()
                LIMIT 1;");
            }

            var inputRef = true;
            _instance.HeClick(("a", "href", "chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/popup.8e8f209b.html\\?windowType=tab&appMode=onboarding#/onboarding/import", "regexp", 0));
            if (source == "pkey")
            {
                _instance.HeClick(("a", "href", "chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/popup.8e8f209b.html\\?windowType=tab&appMode=onboarding#/onboarding/import/private-key", "regexp", 0));
                string key = _key;
                _instance.ActiveTab.FindElementByName("key").SetValue(key, "Full", false);
            }
            else if (source == "seed")
            {
                _instance.HeClick(("a", "href", "chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/popup.8e8f209b.html\\?windowType=tab&appMode=onboarding#/onboarding/import/mnemonic", "regexp", 0));
                string seedPhrase = _seed;
                int index = 0;
                foreach (string word in seedPhrase.Split(' '))
                {
                    _instance.ActiveTab.FindElementById($"word-{index}").SetValue(word, "Full", false);
                    index++;
                }
            }
            _instance.HeClick(("button", "innertext", "Import\\ wallet", "regexp", 0));
            _instance.HeSet(("input:password", "fulltagname", "input:password", "text", 0), _pass);
            _instance.HeClick(("button", "class", "_primary", "regexp", 0));
            _instance.HeSet(("input:password", "fulltagname", "input:password", "text", 0), _pass);
            _instance.HeClick(("button", "class", "_primary", "regexp", 0));
            if (inputRef)
            {
                _instance.HeClick(("button", "innertext", "Enter\\ Referral\\ Code", "regexp", 0));
                _instance.HeSet((("referralCode", "name")), refCode);
                _instance.HeClick(("button", "class", "_regular", "regexp", 0));
            }
            return true;
        }

        public void ZerionUnlock(bool log = false)
        {
            _instance.ActiveTab.Navigate("chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/sidepanel.21ca0c41.html#/overview", "");

            string active = null;
            try
            {
                active = _instance.HeGet(("a", "href", "chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/sidepanel.21ca0c41.html\\#/wallet-select", "regexp", 0),deadline:2);
            }
            catch
            {
                _instance.HeSet(("input:password", "fulltagname", "input:password", "text", 0), _pass);
                _instance.HeClick(("button", "class", "_primary", "regexp", 0));
                active = _instance.HeGet(("a", "href", "chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/sidepanel.21ca0c41.html\\#/wallet-select", "regexp", 0));
            }
            Log(active, log: log);
        }

        public string ZerionCheck(bool log = false)
        {
            if (_instance.ActiveTab.URL != "chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/sidepanel.21ca0c41.html#/overview")
                _instance.ActiveTab.Navigate("chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/sidepanel.21ca0c41.html#/overview", "");

            var active = _instance.HeGet(("div", "class", "_uitext_", "regexp", 0));
            var balance = _instance.HeGet(("div", "class", "_uitext_", "regexp", 1));
            var pnl = _instance.HeGet(("div", "class", "_uitext_", "regexp", 2));

            Log($"{active} {balance} {pnl}", log: log);
            return active;
        }

        public bool ZerionApprove(bool log = false)
        {

            try
            {
                var button = _instance.HeGet(("button", "class", "_primary", "regexp", 0));
                Log(button, log: log);
                _instance.HeClick(("button", "class", "_primary", "regexp", 0));
                return true;
            }
            catch (Exception ex)
            {
                Log($"!W {ex.Message}", log: log);
                throw;
            }
        }

        public void ZerionConnect(bool log = false)
        {

            string action = null;
            getState:

            try
            {
                action = _instance.HeGet(("button", "class", "_primary", "regexp", 0));
            }
            catch (Exception ex)
            {
                _project.L0g($"No Wallet tab found. 0");
                return;
            }

            _project.L0g(action);

            switch (action)
            {
                case "Add":
                    _project.L0g($"adding {_instance.HeGet(("input:url", "fulltagname", "input:url", "text", 0), atr: "value")}");
                    _instance.HeClick(("button", "class", "_primary", "regexp", 0));
                    goto getState;
                case "Close":
                    _project.L0g($"added {_instance.HeGet(("div", "class", "_uitext_", "regexp", 0))}");
                    _instance.HeClick(("button", "class", "_primary", "regexp", 0));
                    goto getState;
                case "Connect":
                    _project.L0g($"connecting {_instance.HeGet(("div", "class", "_uitext_", "regexp", 0))}");
                    _instance.HeClick(("button", "class", "_primary", "regexp", 0));
                    goto getState;
                case "Sign":
                    _project.L0g($"sign {_instance.HeGet(("div", "class", "_uitext_", "regexp", 0))}");
                    _instance.HeClick(("button", "class", "_primary", "regexp", 0));
                    goto getState;

                default:
                    goto getState;

            }


        }

        public bool ZerionWaitTx(int deadline = 60, bool log = false)
        {
            DateTime functionStart = DateTime.Now;
        check:
            bool result;
            if ((DateTime.Now - functionStart).TotalSeconds > deadline) throw new Exception($"!W Deadline [{deadline}]s exeeded");


            if (!_instance.ActiveTab.URL.Contains("chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/sidepanel.21ca0c41.html#/overview/history"))
            {
                Tab tab = _instance.NewTab("zw");
                if (tab.IsBusy) tab.WaitDownloading();
                _instance.ActiveTab.Navigate("chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/sidepanel.21ca0c41.html#/overview/history", "");

            }
            Thread.Sleep(2000);

            var status = _instance.HeGet(("div", "style", "padding: 0px 16px;", "regexp", 0));



            if (status.Contains("Pending")) goto check;
            else if (status.Contains("Failed")) result = false;
            else if (status.Contains("Execute")) result = true;
            else
            {
                Log($"unknown status {status}");
                goto check;
            }
            _instance.CloseExtraTabs();
            return result;

        }
    }

}
