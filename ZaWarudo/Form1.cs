using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WMPLib;

namespace ZaWarudo
{
	public partial class Form1 : Form
	{
		SoundPlayer player;
		JojoHandler handler;
		public Form1()
		{
			InitializeComponent();
			player = new SoundPlayer();
			handler = new JojoHandler(player, new JojoExecutioneer());
		}
		bool isPressed = false;
		CancellationTokenSource ts = new CancellationTokenSource();
		private async void Form1_KeyDown(object sender, KeyEventArgs e)
		{
			if (!isPressed)
			{
				var token = ts.Token;
				isPressed = true;
				player.PlaySound(Sounds.TimeResumes);
				var castTime = handler.OnStartCasting();
				int msCast = (int)(castTime * 1000);
				if (castTime > 2)
					msCast -= 600;
				Debug.WriteLine($"delay {msCast}ms");
				await Task.Delay(msCast, token).ContinueWith(x =>
				{
					if (!x.IsCanceled)
						handler.OnDownCompleted();
					else
						ts = new CancellationTokenSource();
				});
			}
		}
		private void Form1_KeyUp(object sender, KeyEventArgs e)
		{
			handler.OnUpCompleted();
			isPressed = false;
			ts.Cancel();
			ts = new CancellationTokenSource();
		}
	}
	public class JojoHandler
	{
		private State _state;
		public State State
		{
			get => _state;
			set
			{
				_state = value;
				Debug.WriteLine($"State now is {value}");
			}
		}
		private SoundPlayer player;
		private JojoExecutioneer executioneer;
		public JojoHandler(SoundPlayer player, JojoExecutioneer executioneer)
		{
			this.player = player;
			this.executioneer = executioneer;
		}

		internal float OnStartCasting()
		{
			if (State == State.None)
			{
				State = State.CastingZaWarudo;
				return player.PlaySound(Sounds.ZaWarudo);
			}
			else if (State == State.TimeStopped)
			{
				State = State.CastingTokiWaUgokiDesu;
				return player.PlaySound(Sounds.TokiWa);
			}
			else
				throw new Exception("WTF");
		}

		internal void OnUpCompleted()
		{
			if (State == State.CastingZaWarudo)
			{
				State = State.None;
				player.Stop();
			}
			else if (State == State.CastingTokiWaUgokiDesu)
			{
				ResumeTime(false);
				//executioneer.OnTimeResuming(fullyCasted: false);
				//State = State.None;
				//player.Stop();
			}
		}
		internal void OnDownCompleted()//Успешно скастили
		{
			if (State == State.CastingZaWarudo)
			{
				State = State.TimeStopping;
				player.PlaySound(Sounds.TimeStop);

				executioneer.StopTime();//Это типа геймстейта
				State = State.TimeStopped;
			}
			else if (State == State.CastingTokiWaUgokiDesu)
			{
				ResumeTime(true);
			}
		}

		private async Task ResumeTime(bool fullyCasted)
		{
			State = State.TimeResuming;
			executioneer.OnTimeResuming(fullyCasted: fullyCasted);
			var len = player.PlaySound(Sounds.TimeResumes);
			var msDelay = (int)(len * 1000);
			Debug.WriteLine($"delay {msDelay}ms");
			await Task.Delay(msDelay);
			State = State.None;
		}
	}
	public class JojoExecutioneer
	{
		internal void OnTimeResuming(bool fullyCasted)
		{
			Debug.WriteLine($"Resuming Time!!! {fullyCasted}{fullyCasted}{fullyCasted}");
		}

		internal void StopTime()
		{
			Debug.WriteLine("Time is Stopped!!!");
		}
	}
	public enum State
	{
		None,
		CastingZaWarudo,
		TimeStopping,
		TimeStopped,
		CastingTokiWaUgokiDesu,
		TimeResuming
	}
	public class SoundPlayer
	{
		WMPLib.WindowsMediaPlayer Player;
		public SoundPlayer()
		{
			Player = new WMPLib.WindowsMediaPlayer();
		}
		public float PlaySound(Sounds sound)
		{
			string url = $"..\\..\\Sounds\\{sound.ToString()}.mp3";
			string fullUrl = Path.GetFullPath(url);
			return PlayFile(fullUrl);
		}

		internal void Stop()
		{
			Player.controls.stop();
		}

		private float PlayFile(String url)
		{
			var media = Player.newMedia(url);
			Player.URL = url;
			Player.controls.play();
			return (float)media.duration;
		}
	}
	public enum Sounds
	{
		TimeResumes,
		TimeStop,
		ZaWarudo,
		TokiWa
	}
}
