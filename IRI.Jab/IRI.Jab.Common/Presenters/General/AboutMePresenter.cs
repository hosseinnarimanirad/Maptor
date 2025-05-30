﻿using IRI.Jab.Common.Assets.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Jab.Common.Presenters.General;

public class AboutMePresenter
{
    private const string _githubUrl = "https://github.com/hosseinnarimanirad";
    private const string _stackoverflowUrl = "https://stackoverflow.com/users/1468295/hossein-narimani-rad?tab=profile";
    private const string _linkedinUrl = "https://www.linkedin.com/in/hosseinnarimanirad/";
    private const string _makanNegarUrl = "https://hosseinnarimanirad.ir/makanNegar";


    public Action RequestGoToGithub = () => { Process.Start(new ProcessStartInfo(_githubUrl) { UseShellExecute = true, Verb = "open" }); };

    public Action RequestGoToStackoverflow = () => { Process.Start(new ProcessStartInfo(_stackoverflowUrl) { UseShellExecute = true, Verb = "open" }); };

    public Action RequestGoToLinkedin = () => { Process.Start(new ProcessStartInfo(_linkedinUrl) { UseShellExecute = true, Verb = "open" }); };

    public Action RequestGoToMakanNegar = () => { Process.Start(new ProcessStartInfo(_makanNegarUrl) { UseShellExecute = true, Verb = "open" }); };

    private void GoToWebsite(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" });
    }

    private RelayCommand _gotoGithubCommand;
    public RelayCommand GotoGithubCommand
    {
        get
        {
            if (_gotoGithubCommand == null)
            {
                _gotoGithubCommand = new RelayCommand(param => this.RequestGoToGithub?.Invoke());
            }
            return _gotoGithubCommand;
        }
    }


    private RelayCommand _gotoStackoverflowCommand;
    public RelayCommand GotoStackoverflowCommand
    {
        get
        {
            if (_gotoStackoverflowCommand == null)
            {
                _gotoStackoverflowCommand = new RelayCommand(param => this.RequestGoToStackoverflow?.Invoke());
            }
            return _gotoStackoverflowCommand;
        }
    }


    private RelayCommand _gotoLinkedinCommand;
    public RelayCommand GotoLinkedinCommand
    {
        get
        {
            if (_gotoLinkedinCommand == null)
            {
                _gotoLinkedinCommand = new RelayCommand(param => this.RequestGoToLinkedin?.Invoke());
            }
            return _gotoLinkedinCommand;
        }
    }


    private RelayCommand _gotoMakanNegarCommand;

    public RelayCommand GotoMakanNegarCommand
    {
        get
        {
            if (_gotoMakanNegarCommand == null)
            {
                _gotoMakanNegarCommand = new RelayCommand(param => this.RequestGoToMakanNegar?.Invoke());
            }

            return _gotoMakanNegarCommand;
        }
    }

}
