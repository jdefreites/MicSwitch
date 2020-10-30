using System;
using System.Drawing;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;
using log4net;
using MicSwitch.Modularity;
using MicSwitch.Services;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity;

namespace MicSwitch.MainWindow.Models
{
    internal sealed class ImageProvider : DisposableReactiveObject, IImageProvider
    {
        private readonly IMicrophoneControllerEx microphoneController;
        private ImageSource microphoneImage;
        private ImageSource mutedMicrophoneImage;
        private readonly BitmapImage defaultMutedMicrophoneImage = new BitmapImage(new Uri("pack://application:,,,/Resources/microphoneDisabled.ico", UriKind.RelativeOrAbsolute));
        private readonly BitmapImage defaultMicrophoneImage = new BitmapImage(new Uri("pack://application:,,,/Resources/microphoneEnabled.ico", UriKind.RelativeOrAbsolute));

        public ImageProvider(
            [NotNull] IMicrophoneControllerEx microphoneController,
            [NotNull] IConfigProvider<MicSwitchConfig> configProvider,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            this.microphoneController = microphoneController;
            
            configProvider.ListenTo(x => x.MicrophoneLineId)
                .ObserveOn(uiScheduler)
                .Subscribe(lineId => microphoneController.LineId = lineId)
                .AddTo(Anchors);

            Observable.Merge(
                    microphoneController.WhenAnyValue(x => x.Mute).ToUnit(),
                    this.WhenAnyValue(x => x.MicrophoneImage).ToUnit(),
                    this.WhenAnyValue(x => x.MutedMicrophoneImage).ToUnit()
                )
                .ObserveOn(uiScheduler)
                .Subscribe(() => RaisePropertyChanged(nameof(ActiveMicrophoneImage)))
                .AddTo(Anchors);

            configProvider.ListenTo(x => x.MicrophoneIcon)
                .ObserveOn(uiScheduler)
                .SelectSafeOrDefault(x => x.ToBitmapImage())
                .Subscribe(x => MicrophoneImage = x ?? defaultMicrophoneImage)
                .AddTo(Anchors);
            
            configProvider.ListenTo(x => x.MutedMicrophoneIcon)
                .ObserveOn(uiScheduler)
                .SelectSafeOrDefault(x => x.ToBitmapImage())
                .Subscribe(x => MutedMicrophoneImage = x ?? defaultMutedMicrophoneImage)
                .AddTo(Anchors);
        }

        public ImageSource ActiveMicrophoneImage => microphoneController.Mute ?? false ? mutedMicrophoneImage : microphoneImage;

        public ImageSource MicrophoneImage
        {
            get => microphoneImage;
            private set => RaiseAndSetIfChanged(ref microphoneImage, value);
        }

        public ImageSource MutedMicrophoneImage
        {
            get => mutedMicrophoneImage;
            private set => RaiseAndSetIfChanged(ref mutedMicrophoneImage, value);
        }
    }
}