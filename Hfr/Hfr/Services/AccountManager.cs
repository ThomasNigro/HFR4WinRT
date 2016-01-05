﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Hfr.Commands;
using Hfr.Database;
using Hfr.Helpers;
using Hfr.Model;
using Hfr.ViewModel;

namespace Hfr.Services
{
    public class AccountManager
    {
        private AccountDataRepository accountDataRepository = new AccountDataRepository();
        private ConnectCommand connectCommand = new ConnectCommand();
        public List<Account> Accounts = new List<Account>();
        public Account CurrentAccount { get; set; }
        public ConnectCommand ConnectCommand { get { return connectCommand; } }
        
        public AccountManager()
        {
            Initialize();
        }

        async Task Initialize()
        {
            var accounts = accountDataRepository?.GetAccounts();
            if (accounts != null && accounts.Any())
            {
                Accounts = accounts;
                if (accounts.Count == 1)
                {
                    CurrentAccount = accounts[0];
                    Loc.NavigationService.Navigate(View.Connect);
#warning "Optimistic: any account/cookies is supposed valid, proper implementation needed (cookies timeout check)"
                    await ThreadUI.Invoke(() =>
                    {
                        Loc.Main.AccountManager.CurrentAccount.ConnectionErrorStatus = "Connecting";
                        Loc.Main.AccountManager.CurrentAccount.IsConnecting = true;
                    });
                    bool success = await CurrentAccount.BeginAuthentication(false);
                    if (success)
                    {
                        await ThreadUI.Invoke(() =>
                        {
                            Loc.Main.AccountManager.CurrentAccount.IsConnected = false;
                            Loc.NavigationService.Navigate(View.Main);
                        });
                    }
                    else
                    {
                        Debug.WriteLine("Login failed");
                    }
                }
                else
                {
                    // Handle seletion of one of the multi accounts
                }
            }
            else
            {
                // navigate to connectpage
                await ThreadUI.Invoke(() =>
                {
                    CurrentAccount = new Account();
                    Loc.NavigationService.Navigate(View.Connect);
                    // Trying to detect login and password stored in RoamingSettings
                    if (ApplicationSettingsHelper.Contains(nameof(CurrentAccount.Pseudo), false) && ApplicationSettingsHelper.Contains(nameof(CurrentAccount.Password), false))
                    {
                        ToastHelper.Simple("Connexion automatique ...");
                        var pseudo = ApplicationSettingsHelper.ReadSettingsValue(nameof(CurrentAccount.Pseudo), false).ToString();
                        var password = ApplicationSettingsHelper.ReadSettingsValue(nameof(CurrentAccount.Password), false).ToString();
                        CurrentAccount.Pseudo = pseudo;
                        CurrentAccount.Password = password;
                        connectCommand.Execute(null);
                    }
                });
            }
        }

        public void UpdateCurrentAccountInDB()
        {
            accountDataRepository?.Update(CurrentAccount);
        }

        internal void AddCurrentAccountInDB()
        {
            accountDataRepository?.Add(CurrentAccount);
        }

        internal void AddCurrentAccountInRoamingSettings()
        {
            ApplicationSettingsHelper.SaveSettingsValue(nameof(CurrentAccount.Pseudo), CurrentAccount.Pseudo, false);
            ApplicationSettingsHelper.SaveSettingsValue(nameof(CurrentAccount.Password), CurrentAccount.Password, false);
        }
    }
}