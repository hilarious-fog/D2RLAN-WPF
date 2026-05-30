using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using D2RLAN.Extensions;
using D2RLAN.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using JetBrains.Annotations;
using SevenZip;
using ILog = log4net.ILog;
using LogManager = log4net.LogManager;

namespace D2RLAN.ViewModels.Dialogs;

public class DownloadNewModViewModel : Caliburn.Micro.Screen
{
    #region ---Static Members---

    private ILog _logger = LogManager.GetLogger(typeof(DownloadNewModViewModel));
    private ObservableCollection<KeyValuePair<string, string>> _mods = new ObservableCollection<KeyValuePair<string, string>>();
    private readonly Dictionary<string, string> _modInfoLinks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private string _serviceAccountEmail;
    private string _privateKey; 
    private KeyValuePair<string, string> _selectedMod;
    private string _modDownloadLink;
    private string _modInfoLink;
    private double _downloadProgress;
    private bool _progressBarIsIndeterminate;
    private string _progressStatus;
    private string _downloadProgressString;

    #endregion

    #region ---Window/Loaded Handlers---

    public DownloadNewModViewModel()
    {
        if (Execute.InDesignMode)
        {
            DownloadProgressString = "70%";
            ProgressStatus = "Test Progress Status...";
            SelectedMod = new KeyValuePair<string, string>("Text Mod", "This is a test string");
            ModDownloadLink = "This is a test string";
        }
    }
    public DownloadNewModViewModel(ShellViewModel shellViewModel)
    {
        DisplayName = "Download A New Mod";
        ShellViewModel = shellViewModel;

        _serviceAccountEmail = ShellViewModel.Configuration["ServiceAccountEmail"] ?? string.Empty;
        _privateKey = ShellViewModel.Configuration["PrivateKey"] ?? string.Empty;

        if (string.IsNullOrEmpty(_serviceAccountEmail) || string.IsNullOrEmpty(_privateKey))
        {
            MessageBox.Show("Please make sure appSettings.json has been properly setup!");
            return;
        }

        Execute.OnUIThread(async () =>
        {
            await GetAvailableMods();

            if (ShellViewModel.UserSettings.DataHashPass == false && SelectedMod.Key == "TCP Files (Install First)")
                OnInstallMod();
        });
    }

    #endregion

    #region ---Properties---

    public string ProgressStatus
    {
        get => _progressStatus;
        set
        {
            if (value == _progressStatus) return;
            _progressStatus = value;
            NotifyOfPropertyChange();
        }
    }
    public bool ProgressBarIsIndeterminate
    {
        get => _progressBarIsIndeterminate;
        set
        {
            if (value == _progressBarIsIndeterminate) return;
            _progressBarIsIndeterminate = value;
            NotifyOfPropertyChange();
        }
    }
    public string DownloadProgressString
    {
        get => _downloadProgressString;
        set
        {
            if (value == _downloadProgressString) return;
            _downloadProgressString = value;
            NotifyOfPropertyChange();
        }
    }
    public double DownloadProgress
    {
        get => _downloadProgress;
        set
        {
            if (value.Equals(_downloadProgress)) return;
            _downloadProgress = value;
            NotifyOfPropertyChange();
        }
    }
    public string ModDownloadLink
    {
        get => _modDownloadLink;
        set
        {
            if (value == _modDownloadLink) return;
            _modDownloadLink = value;
            NotifyOfPropertyChange();
        }
    }
    public string ModInfoLink
    {
        get => _modInfoLink;
        set
        {
            if (value == _modInfoLink) return;
            _modInfoLink = value;
            NotifyOfPropertyChange();
        }
    }
    public KeyValuePair<string, string> SelectedMod
    {
        get => _selectedMod;
        set
        {
            if (value.Equals(_selectedMod)) return;
            _selectedMod = value;
            NotifyOfPropertyChange();
            UpdateModInfoLink();
        }
    }
    public ObservableCollection<KeyValuePair<string, string>> Mods
    {
        get => _mods;
        set
        {
            if (Equals(value, _mods))
            {
                return;
            }
            _mods = value;
            NotifyOfPropertyChange();
        }
    }
    public ShellViewModel ShellViewModel { get; }

    #endregion

    #region ---Download Mod Functions---

    private async Task GetAvailableMods()
    {
        Mods.Clear();

        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.000") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.001"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/xfauuwisdrazv15en3le9/part_2.zip?rlkey=7z54q10sj8bc6joh64monnt8e&st=pdtn5lam&dl=1");
            _logger.Info("TCP FILES: Part 1 files found in game folder, skipping to part 2");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.001") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.002"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/f8uk62rfe13g6mxyg39o6/part_3.zip?rlkey=r9ajcp3c1qqypxvoassm9txpz&st=eeqrnk98&dl=1");
            _logger.Info("TCP FILES: Part 2 files found in game folder, skipping to part 3");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.002") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.003"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/ymp5swx1d3c7dhclk58bg/part_4.zip?rlkey=y2nq4gglb2wzez2tjlfc9a7if&st=xwv5jfk1&dl=1");
            _logger.Info("TCP FILES: Part 3 files found in game folder, skipping to part 4");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.003") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.004"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/yp73pf9usrfkijirsb8bq/part_5.zip?rlkey=we22ry0bcyq1feyg9thqhi61x&st=unck9lcr&dl=1");
            _logger.Info("TCP FILES: Part 4 files found in game folder, skipping to part 5");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.004") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.005"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/5n1sniikhbrxkuyaxkxnx/part_6.zip?rlkey=tjlfflcemq0qks53dt1hsp29y&st=l9q24mbl&dl=1");
            _logger.Info("TCP FILES: Part 5 files found in game folder, skipping to part 6");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.005") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.006"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/a7iupij9kp38g20l7uh3f/part_7.zip?rlkey=s2uhob8xisw35stqsnkarj6d8&st=3fer5z78&dl=1");
            _logger.Info("TCP FILES: Part 6 files found in game folder, skipping to part 7");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.006") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.007"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/bhc2xcz4lr89wmdwf9zab/part_8.zip?rlkey=y26ifpnofgonw8tbay5eqvrky&st=56e9f0x5&dl=1");
            _logger.Info("TCP FILES: Part 7 files found in game folder, skipping to part 8");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.007") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.008"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/7szp2x08l9mihvwx6keim/part_9.zip?rlkey=qvn97yhvx71xsnabc07kj85w4&st=r6sj03nr&dl=1");
            _logger.Info("TCP FILES: Part 8 files found in game folder, skipping to part 9");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.008") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.009"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/shx38qac2w8vp5m3g8qx2/part_10.zip?rlkey=f166orls9wf4753nom9ehk9pg&st=lh531ivj&dl=1");
            _logger.Info("TCP FILES: Part 9 files found in game folder, skipping to part 10");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.009") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.010"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/kowzlaor8b4fgrzrz32i7/part_11.zip?rlkey=miua5ysyi27downdt1c8wckt3&st=zklplkgi&dl=1");
            _logger.Info("TCP FILES: Part 10 files found in game folder, skipping to part 11");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.010") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.011"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/z0eiint7e4gp1wvr8xlpp/part_12.zip?rlkey=28lad4j0zp1nlgkj14lzg04gr&st=0fdrfkn4&dl=1");
            _logger.Info("TCP FILES: Part 11 files found in game folder, skipping to part 12");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.011") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.012"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/n67680s3gmdq86v0px3p1/part_13.zip?rlkey=m619htc0ldkzjgu884juee7ub&st=80s6znzk&dl=1");
            _logger.Info("TCP FILES: Part 12 files found in game folder, skipping to part 13");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.012") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.013"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/t8ze6fyhvi64n5wcsalbw/part_14.zip?rlkey=rmj63g6r7vmybz4y1skc4mbtc&st=clwazzl7&dl=1");
            _logger.Info("TCP FILES: Part 13 files found in game folder, skipping to part 14");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.013") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.014"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/vo69ioboy322mjkg7w1i1/part_15.zip?rlkey=yt01qdluv1p8ysvhhb5tlyrp4&st=86kh06zb&dl=1");
            _logger.Info("TCP FILES: Part 14 files found in game folder, skipping to part 15");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.014") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.015"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/q62mvxvnzu4wo9tlpkfs6/part_16.zip?rlkey=tm3exwg2mlod6k0r8wmwk2ss7&st=8w9vsg6z&dl=1");
            _logger.Info("TCP FILES: Part 15 files found in game folder, skipping to part 16");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.015") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.016"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/8qpdus1cgobnk9vxnzyxy/part_17.zip?rlkey=nwlao67c1fcc0gxypbjskd606&st=2vbuapgv&dl=1");
            _logger.Info("TCP FILES: Part 16 files found in game folder, skipping to part 17");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.016") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.017"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/862rol42uj8wf29h86pl7/part_18.zip?rlkey=61z4rwv975hs6w41k6qxbexq4&st=rtcgw3ua&dl=1");
            _logger.Info("TCP FILES: Part 17 files found in game folder, skipping to part 18");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.017") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.018"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/bo86sojtipupe2uassrkv/part_19.zip?rlkey=osjqfyyqwnop8owicin3cuqdh&st=nmxhxdde&dl=1");
            _logger.Info("TCP FILES: Part 18 files found in game folder, skipping to part 19");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.018") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.019"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/wa8p39mf2eeyvsvmsrh8s/part_20.zip?rlkey=1hlu3xsw6oeskdbcwu1vov0ce&st=ja978k2e&dl=1");
            _logger.Info("TCP FILES: Part 19 files found in game folder, skipping to part 20");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.019") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.020"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/g1e6pz0lsmqk94jijml93/part_21.zip?rlkey=alisfgqxz05avl0auxct1bh45&st=kexxsgfl&dl=1");
            _logger.Info("TCP FILES: Part 20 files found in game folder, skipping to part 21");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.020") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.021"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/bb0zwjn7heppk1t85hh7p/part_22.zip?rlkey=zkguk4pdb6l30bixf4o8vkhom&st=8y59icge&dl=1");
            _logger.Info("TCP FILES: Part 21 files found in game folder, skipping to part 22");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.021") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.022"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/ly9iuc8v9flqhrogosmac/part_23.zip?rlkey=wnlr9xxso67y8e0p9pts5vf6d&st=nant3hd2&dl=1");
            _logger.Info("TCP FILES: Part 22 files found in game folder, skipping to part 23");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.022") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.023"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/wjdtbg5wdgx0v0qu9ka0q/part_24.zip?rlkey=uajomuuo2c8a3f7ljpmeqbai5&st=8pzck1ro&dl=1");
            _logger.Info("TCP FILES: Part 23 files found in game folder, skipping to part 24");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.023") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.024"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/d1nwopfvvbnkoncps75j6/part_25.zip?rlkey=fkyga0ifgqvnc6dm4twzy3mzx&st=33nwtcor&dl=1");
            _logger.Info("TCP FILES: Part 24 files found in game folder, skipping to part 25");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.024") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.025"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/g5a788wrkpfdpl72xofkj/part_26.zip?rlkey=yd7bhgx7k79m0djjld1ves3h8&st=c5s21pe9&dl=1");
            _logger.Info("TCP FILES: Part 25 files found in game folder, skipping to part 26");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.025") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.026"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", "https://www.dropbox.com/scl/fi/r0m5253h3g05qa8o3y4d4/part_27.zip?rlkey=mwzsyv8dz3mr6ioj27fn80hp5&st=l00w6lyo&dl=1");
            _logger.Info("TCP FILES: Part 26 files found in game folder, skipping to part 27");

            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;
            return;
        }
        else if (!File.Exists($@"{ShellViewModel.GamePath}data\data\data.000"))
        {
            var tcpEntry = new KeyValuePair<string, string>("TCP Files (Install First)", string.Join(",", new[]
            {
                "https://www.dropbox.com/scl/fi/x9kuz93yxwo5mltq9hdoh/part_1.zip?rlkey=qye0qnrrs426rbd7vgcfetu5h&st=033aw4kl&dl=1",
                "https://www.dropbox.com/scl/fi/xfauuwisdrazv15en3le9/part_2.zip?rlkey=7z54q10sj8bc6joh64monnt8e&st=pdtn5lam&dl=1",
                "https://www.dropbox.com/scl/fi/f8uk62rfe13g6mxyg39o6/part_3.zip?rlkey=r9ajcp3c1qqypxvoassm9txpz&st=eeqrnk98&dl=1",
                "https://www.dropbox.com/scl/fi/ymp5swx1d3c7dhclk58bg/part_4.zip?rlkey=y2nq4gglb2wzez2tjlfc9a7if&st=xwv5jfk1&dl=1",
                "https://www.dropbox.com/scl/fi/yp73pf9usrfkijirsb8bq/part_5.zip?rlkey=we22ry0bcyq1feyg9thqhi61x&st=unck9lcr&dl=1",
                "https://www.dropbox.com/scl/fi/5n1sniikhbrxkuyaxkxnx/part_6.zip?rlkey=tjlfflcemq0qks53dt1hsp29y&st=l9q24mbl&dl=1",
                "https://www.dropbox.com/scl/fi/a7iupij9kp38g20l7uh3f/part_7.zip?rlkey=s2uhob8xisw35stqsnkarj6d8&st=3fer5z78&dl=1",
                "https://www.dropbox.com/scl/fi/bhc2xcz4lr89wmdwf9zab/part_8.zip?rlkey=y26ifpnofgonw8tbay5eqvrky&st=56e9f0x5&dl=1",
                "https://www.dropbox.com/scl/fi/7szp2x08l9mihvwx6keim/part_9.zip?rlkey=qvn97yhvx71xsnabc07kj85w4&st=r6sj03nr&dl=1",
                "https://www.dropbox.com/scl/fi/shx38qac2w8vp5m3g8qx2/part_10.zip?rlkey=f166orls9wf4753nom9ehk9pg&st=lh531ivj&dl=1",
                "https://www.dropbox.com/scl/fi/kowzlaor8b4fgrzrz32i7/part_11.zip?rlkey=miua5ysyi27downdt1c8wckt3&st=zklplkgi&dl=1",
                "https://www.dropbox.com/scl/fi/z0eiint7e4gp1wvr8xlpp/part_12.zip?rlkey=28lad4j0zp1nlgkj14lzg04gr&st=0fdrfkn4&dl=1",
                "https://www.dropbox.com/scl/fi/n67680s3gmdq86v0px3p1/part_13.zip?rlkey=m619htc0ldkzjgu884juee7ub&st=80s6znzk&dl=1",
                "https://www.dropbox.com/scl/fi/t8ze6fyhvi64n5wcsalbw/part_14.zip?rlkey=rmj63g6r7vmybz4y1skc4mbtc&st=clwazzl7&dl=1",
                "https://www.dropbox.com/scl/fi/vo69ioboy322mjkg7w1i1/part_15.zip?rlkey=yt01qdluv1p8ysvhhb5tlyrp4&st=86kh06zb&dl=1",
                "https://www.dropbox.com/scl/fi/q62mvxvnzu4wo9tlpkfs6/part_16.zip?rlkey=tm3exwg2mlod6k0r8wmwk2ss7&st=8w9vsg6z&dl=1",
                "https://www.dropbox.com/scl/fi/8qpdus1cgobnk9vxnzyxy/part_17.zip?rlkey=nwlao67c1fcc0gxypbjskd606&st=2vbuapgv&dl=1",
                "https://www.dropbox.com/scl/fi/862rol42uj8wf29h86pl7/part_18.zip?rlkey=61z4rwv975hs6w41k6qxbexq4&st=rtcgw3ua&dl=1",
                "https://www.dropbox.com/scl/fi/bo86sojtipupe2uassrkv/part_19.zip?rlkey=osjqfyyqwnop8owicin3cuqdh&st=nmxhxdde&dl=1",
                "https://www.dropbox.com/scl/fi/wa8p39mf2eeyvsvmsrh8s/part_20.zip?rlkey=1hlu3xsw6oeskdbcwu1vov0ce&st=ja978k2e&dl=1",
                "https://www.dropbox.com/scl/fi/g1e6pz0lsmqk94jijml93/part_21.zip?rlkey=alisfgqxz05avl0auxct1bh45&st=kexxsgfl&dl=1",
                "https://www.dropbox.com/scl/fi/bb0zwjn7heppk1t85hh7p/part_22.zip?rlkey=zkguk4pdb6l30bixf4o8vkhom&st=8y59icge&dl=1",
                "https://www.dropbox.com/scl/fi/ly9iuc8v9flqhrogosmac/part_23.zip?rlkey=wnlr9xxso67y8e0p9pts5vf6d&st=nant3hd2&dl=1",
                "https://www.dropbox.com/scl/fi/wjdtbg5wdgx0v0qu9ka0q/part_24.zip?rlkey=uajomuuo2c8a3f7ljpmeqbai5&st=8pzck1ro&dl=1",
                "https://www.dropbox.com/scl/fi/d1nwopfvvbnkoncps75j6/part_25.zip?rlkey=fkyga0ifgqvnc6dm4twzy3mzx&st=33nwtcor&dl=1",
                "https://www.dropbox.com/scl/fi/g5a788wrkpfdpl72xofkj/part_26.zip?rlkey=yd7bhgx7k79m0djjld1ves3h8&st=c5s21pe9&dl=1",
                "https://www.dropbox.com/scl/fi/r0m5253h3g05qa8o3y4d4/part_27.zip?rlkey=mwzsyv8dz3mr6ioj27fn80hp5&st=l00w6lyo&dl=1"
            }));
            _logger.Info("TCP FILES: Files not found, downloading all 27 parts...");
            Mods.Add(tcpEntry);
            SelectedMod = tcpEntry;

            return;
        }
        else
        {
            try
            {
                // Create credentials
                ServiceAccountCredential serviceAccountCredential = new(new ServiceAccountCredential.Initializer(_serviceAccountEmail)
                {
                    Scopes = new[] { SheetsService.Scope.Spreadsheets }
                }.FromPrivateKey(_privateKey));

                // Create Google Sheets service
                SheetsService sheetsService = new SheetsService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = serviceAccountCredential,
                    ApplicationName = "D2RLaunch"
                });

                // Define spreadsheetId and ranges
                string spreadsheetId = "1ICm2wxCTrQrgRxPJshj1WPA10-slATymYLm7WYkmkis";
                string columnModName = "Sheet1!B10:B";
                string columnModLink = "Sheet1!E10:E";
                string columnModInfoLink = "Sheet1!L10:L";

                // Fetch values from Google Sheets for column with mod names
                SpreadsheetsResource.ValuesResource.GetRequest request =
                    sheetsService.Spreadsheets.Values.Get(spreadsheetId, columnModName);

                ValueRange response = await request.ExecuteAsync();
                IList<IList<object>> dValues = response.Values ?? Array.Empty<IList<object>>();

                // Fetch values from Google Sheets for column with download links
                SpreadsheetsResource.ValuesResource.GetRequest request2 =
                    sheetsService.Spreadsheets.Values.Get(spreadsheetId, columnModLink);

                response = await request2.ExecuteAsync();
                IList<IList<object>> gValues = response.Values ?? Array.Empty<IList<object>>();

                // Fetch values from Google Sheets for column with mod info/wiki links
                SpreadsheetsResource.ValuesResource.GetRequest request3 =
                    sheetsService.Spreadsheets.Values.Get(spreadsheetId, columnModInfoLink);

                response = await request3.ExecuteAsync();
                IList<IList<object>> lValues = response.Values ?? Array.Empty<IList<object>>();

                if (dValues.Count != gValues.Count || dValues.Count != lValues.Count)
                {
                    System.Windows.MessageBox.Show(
                        "The number of items in the mod name, download link, and mod info link columns do not match.\nPlease notify an admin.",
                        "Column Mismatch!", MessageBoxButton.OK, MessageBoxImage.Error);
                    _logger.Error("The number of items in the mod name, download link, and mod info link columns do not match.");
                    return;
                }

                Mods.Clear();
                _modInfoLinks.Clear();
                for (int i = 0; i < dValues.Count; i++)
                {
                    var modName = dValues[i].Count > 0 ? dValues[i][0].ToString() : string.Empty;
                    var modLink = gValues[i].Count > 0 ? gValues[i][0].ToString() : string.Empty;
                    var modInfo = lValues.Count > i && lValues[i].Count > 0 ? lValues[i][0].ToString() : string.Empty;

                    if (string.IsNullOrWhiteSpace(modName))
                        continue;

                    Mods.Add(new KeyValuePair<string, string>(modName, modLink));

                    if (!string.IsNullOrWhiteSpace(modInfo))
                        _modInfoLinks[modName] = modInfo;
                }

                // Automatically assign first entry to SelectedMod
                if (Mods.Count > 0)
                {
                    SelectedMod = Mods[0];
                    UpdateModInfoLink();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _logger.Error(ex);
            }
        }
    }

    [UsedImplicitly]
    public async void OnInstallMod()
    {
        if (SelectedMod.Key != "TCP Files (Install First)")
            ModDownloadLink = ModDownloadLink.TrimEnd();

        string tempPath = Path.GetTempPath();
        string tempExtractedModFolderPath = Path.Combine(tempPath, "NewModDownload");
        SevenZipExtractor.SetLibraryPath("7z.dll");

        try
        {
            if (Directory.Exists(tempExtractedModFolderPath))
                Directory.Delete(tempExtractedModFolderPath, true);

            // === Branch: TCP special handling ===
            if (SelectedMod.Key == "TCP Files (Install First)")
            {
                var links = SelectedMod.Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                int fileIndex = 1;

                foreach (var link in links)
                {
                    string tempFile = Path.Combine(ShellViewModel.GamePath, $"BaseTCPFiles_Part{fileIndex}.zip");

                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = Timeout.InfiniteTimeSpan;

                        var response = await client.GetAsync(link.Trim(), HttpCompletionOption.ResponseHeadersRead);
                        response.EnsureSuccessStatusCode();
                        var contentLength = response.Content.Headers.ContentLength ?? -1L;

                        await using var httpStream = await response.Content.ReadAsStreamAsync();
                        await using var file = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);

                        byte[] buffer = new byte[81920];
                        long totalRead = 0;
                        int read;
                        var sw = Stopwatch.StartNew();

                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.000") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.001"))
                            fileIndex = 2;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.001") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.002"))
                            fileIndex = 3;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.002") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.003"))
                            fileIndex = 4;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.003") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.004"))
                            fileIndex = 5;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.004") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.005"))
                            fileIndex = 6;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.005") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.006"))
                            fileIndex = 7;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.006") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.007"))
                            fileIndex = 8;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.007") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.008"))
                            fileIndex = 9;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.008") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.009"))
                            fileIndex = 10;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.009") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.010"))
                            fileIndex = 11;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.010") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.011"))
                            fileIndex = 12;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.011") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.012"))
                            fileIndex = 13;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.012") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.013"))
                            fileIndex = 14;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.013") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.014"))
                            fileIndex = 15;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.014") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.015"))
                            fileIndex = 16;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.015") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.016"))
                            fileIndex = 17;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.016") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.017"))
                            fileIndex = 18;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.017") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.018"))
                            fileIndex = 19;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.018") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.019"))
                            fileIndex = 20;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.019") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.020"))
                            fileIndex = 21;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.020") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.021"))
                            fileIndex = 22;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.021") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.022"))
                            fileIndex = 23;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.022") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.023"))
                            fileIndex = 24;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.023") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.024"))
                            fileIndex = 25;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.024") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.025"))
                            fileIndex = 26;
                        if (File.Exists($@"{ShellViewModel.GamePath}data\data\data.025") && !File.Exists($@"{ShellViewModel.GamePath}data\data\data.026"))
                            fileIndex = 27;

                        ProgressBarIsIndeterminate = false;
                        ProgressStatus = $"Downloading part {fileIndex} of 27...";

                        while ((read = await httpStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await file.WriteAsync(buffer, 0, read);
                            totalRead += read;

                            double speed = totalRead / Math.Max(0.0001, sw.Elapsed.TotalSeconds);
                            string speedStr = $"{speed / 1024d / 1024d:0.00} MB/s";

                            if (contentLength > 0)
                            {
                                double progress = (double)totalRead / contentLength * 100.0;
                                double remainingSeconds = (contentLength - totalRead) / Math.Max(1, speed);
                                string timeRemaining = remainingSeconds > 0 ? $"{TimeSpan.FromSeconds(remainingSeconds):mm\\:ss}" : "--:--";

                                Execute.OnUIThread(() =>
                                {
                                    DownloadProgress = Math.Round(progress, MidpointRounding.AwayFromZero);
                                    DownloadProgressString = $"{DownloadProgress}%  " + $"({totalRead / 1024d / 1024d:0} / {contentLength / 1024d / 1024d:0} MB)  " + $"{speedStr}  ETA: {timeRemaining}";
                                });
                            }
                            else
                            {
                                // Unknown content-length: show bytes + speed
                                Execute.OnUIThread(() =>
                                {
                                    DownloadProgressString = $"{totalRead / 1024d / 1024d:0} MB downloaded  {speedStr}";
                                });
                            }
                        }

                        file.Close();
                        sw.Stop();
                    }

                    ProgressStatus = $"Extracting part {fileIndex} of 6...";
                    ProgressBarIsIndeterminate = true;

                    // Special extraction: strip root folder
                    await Task.Run(() =>
                    {
                        if (tempFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var archive = ZipFile.OpenRead(tempFile))
                            {
                                foreach (var entry in archive.Entries)
                                {
                                    if (string.IsNullOrEmpty(entry.FullName))
                                        continue;

                                    string relativePath = entry.FullName;
                                    int slashIndex = relativePath.IndexOf('/');
                                    if (slashIndex >= 0)
                                        relativePath = relativePath.Substring(slashIndex + 1);

                                    if (string.IsNullOrWhiteSpace(relativePath))
                                        continue;

                                    string destinationPath = Path.Combine(ShellViewModel.GamePath, relativePath);
                                    string destDir = Path.GetDirectoryName(destinationPath);
                                    if (!string.IsNullOrEmpty(destDir))
                                        Directory.CreateDirectory(destDir);

                                    // Skip directory entries
                                    if (!entry.FullName.EndsWith("/"))
                                        entry.ExtractToFile(destinationPath, true);
                                }
                            }
                        }
                    });

                    File.Delete(tempFile);
                    fileIndex++;
                }

                // mark as fully downloaded for UI
                Execute.OnUIThread(() =>
                {
                    DownloadProgress = 100;
                    DownloadProgressString = "Download complete.";
                });
            }
            // === Branch: Normal mods (single file, but with progress) ===
            else
            {
                string tempFile = Path.Combine(tempPath, "NewModDownload.zip");

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;

                    var response = await client.GetAsync(SelectedMod.Value.Trim(), HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    var contentLength = response.Content.Headers.ContentLength ?? -1L;

                    await using var httpStream = await response.Content.ReadAsStreamAsync();
                    await using var file = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);

                    byte[] buffer = new byte[81920];
                    long totalRead = 0;
                    int read;
                    var sw = Stopwatch.StartNew();

                    ProgressBarIsIndeterminate = false;
                    ProgressStatus = "Downloading mod...";

                    while ((read = await httpStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await file.WriteAsync(buffer, 0, read);
                        totalRead += read;

                        double speed = totalRead / Math.Max(0.0001, sw.Elapsed.TotalSeconds);
                        string speedStr = $"{speed / 1024d / 1024d:0.00} MB/s";

                        if (contentLength > 0)
                        {
                            double progress = (double)totalRead / contentLength * 100.0;
                            double remainingSeconds = (contentLength - totalRead) / Math.Max(1, speed);
                            string timeRemaining = remainingSeconds > 0
                                ? $"{TimeSpan.FromSeconds(remainingSeconds):mm\\:ss}"
                                : "--:--";

                            Execute.OnUIThread(() =>
                            {
                                DownloadProgress = Math.Round(progress, MidpointRounding.AwayFromZero);
                                DownloadProgressString =
                                    $"{DownloadProgress}%  " +
                                    $"({totalRead / 1024d / 1024d:0} / {contentLength / 1024d / 1024d:0} MB)  " +
                                    $"{speedStr}  ETA: {timeRemaining}";
                            });
                        }
                        else
                        {
                            Execute.OnUIThread(() =>
                            {
                                DownloadProgressString =
                                    $"{totalRead / 1024d / 1024d:0} MB downloaded  {speedStr}";
                            });
                        }
                    }

                    file.Close();
                    sw.Stop();
                    Execute.OnUIThread(() =>
                    {
                        DownloadProgress = 100;
                        DownloadProgressString = "Download complete.";
                    });
                }

                ProgressStatus = "Extracting mod...";
                ProgressBarIsIndeterminate = true;

                if (tempFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    ZipFile.ExtractToDirectory(tempFile, tempExtractedModFolderPath, true);
                else
                {
                    using var extractor = new SevenZipExtractor(tempFile);
                    extractor.ExtractArchive(tempExtractedModFolderPath);
                }

                File.Delete(tempFile);
            }

            if (Directory.Exists(tempExtractedModFolderPath))
            {
                // === Remainder of function (install and cleanup) ===
                string tempModDirPath = await Helper.FindFolderWithMpq(tempExtractedModFolderPath);
                string tempModDir = Path.GetFileName(tempModDirPath);
                string tempParentDir = Path.GetDirectoryName(tempModDirPath);
                string modName = string.Empty;

                if (tempModDir != null)
                    modName = tempModDir.Replace(".mpq", "");
                else
                {
                    MessageBox.Show("Mod download was unsuccessful", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string modInstallPath = Path.Combine(ShellViewModel.BaseModsFolder, modName);

                if (File.Exists(Path.Combine(ShellViewModel.SelectedModDataFolder, @"global\ui\layouts\bankexpansionlayouthd.json")))
                    File.Copy(Path.Combine(ShellViewModel.SelectedModDataFolder, @"global\ui\layouts\bankexpansionlayouthd.json"), Path.Combine(ShellViewModel.BaseModsFolder, "temp_bankexpansionlayouthd.json"), true);

                //Delete current Mod folder if it exists
                if (Directory.Exists(modInstallPath))
                {
                    if (File.Exists(Path.Combine(modInstallPath, $@"{modName}.mpq\MyUserSettings.json")))
                        File.Move(Path.Combine(modInstallPath, $@"{modName}.mpq\MyUserSettings.json"), Path.Combine(ShellViewModel.BaseModsFolder, "MyUserSettings.json"));
                    Directory.Delete(modInstallPath, true);
                }

                ProgressStatus = "Installing mod...";

                await Task.Run(async () =>
                {
                    await Helper.CloneDirectory(tempParentDir, modInstallPath);
                });

                string versionPath = Path.Combine(modInstallPath, "version.txt");

                if (!File.Exists(versionPath))
                    File.Create(versionPath).Close();

                string tempModInfoPath = Path.Combine(tempModDirPath, "modinfo.json");
                ModInfo modInfo = await Helper.ParseModInfo(tempModInfoPath);

                if (modInfo != null)
                    await File.WriteAllTextAsync(versionPath, modInfo.ModVersion);
                else
                    MessageBox.Show("Could not parse ModInfo.json!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Clean up temp files
                if (Directory.Exists(tempExtractedModFolderPath))
                    Directory.Delete(tempExtractedModFolderPath, true);
                if (File.Exists(Path.Combine(ShellViewModel.BaseModsFolder, "MyUserSettings.json")))
                    File.Move(Path.Combine(ShellViewModel.BaseModsFolder, "MyUserSettings.json"), Path.Combine(modInstallPath, $@"{modName}.mpq\MyUserSettings.json"));
                ProgressStatus = "Installing Complete!";

                if (File.Exists(Path.Combine(ShellViewModel.BaseModsFolder, "temp_bankexpansionlayouthd.json")))
                {
                    File.Copy(Path.Combine(ShellViewModel.BaseModsFolder, "temp_bankexpansionlayouthd.json"), Path.Combine(ShellViewModel.SelectedModDataFolder, @"global\ui\layouts\bankexpansionlayouthd.json"), true);
                    File.Delete(Path.Combine(ShellViewModel.BaseModsFolder, "temp_bankexpansionlayouthd.json"));
                }

                MessageBox.Show($"{modName} has been installed!", "Mod Installed!", MessageBoxButton.OK, MessageBoxImage.None);

                // We installed a custom mod from a direct link 
                if (string.IsNullOrEmpty(SelectedMod.Key))
                    SelectedMod = new KeyValuePair<string, string>(modName, "DirectDownload");

                await TryCloseAsync(true);
            }
            else
            {
                ProgressStatus = "Install Complete!";
                MessageBox.Show($"TCP Base Files have been installed!", "Base Files Installed!", MessageBoxButton.OK, MessageBoxImage.None);

                // We installed a custom mod from a direct link 
                if (string.IsNullOrEmpty(SelectedMod.Key))
                    SelectedMod = new KeyValuePair<string, string>("TCP", "DirectDownload");

                await TryCloseAsync(true);
            }
            
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            _logger.Error(ex);

            if (Directory.Exists(tempExtractedModFolderPath))
                Directory.Delete(tempExtractedModFolderPath, true);

            await TryCloseAsync(false);
        }
    }

    [UsedImplicitly]
    public async void OnModInstallSelectionChanged()
    {
        if (!string.IsNullOrEmpty(SelectedMod.Value))
        {
            if (SelectedMod.Key != "TCP Files (Install First)")
                ModDownloadLink = SelectedMod.Value;
        }
        UpdateModInfoLink();
    }

    private void UpdateModInfoLink()
    {
        if (SelectedMod.Key == "TCP Files (Install First)" || string.IsNullOrWhiteSpace(SelectedMod.Key))
        {
            ModInfoLink = string.Empty;
            return;
        }

        if (_modInfoLinks.TryGetValue(SelectedMod.Key, out var infoLink))
            ModInfoLink = infoLink;
        else
            ModInfoLink = string.Empty;
    }

    [UsedImplicitly]
    public void OnOpenModInfo()
    {
        const string noInfoText = "No Info Provided (Yet)";

        if (string.IsNullOrWhiteSpace(ModInfoLink) ||
            string.Equals(ModInfoLink, noInfoText, StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ModInfoLink,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to open mod info link.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            _logger.Error(ex);
        }
    }

    #endregion
}