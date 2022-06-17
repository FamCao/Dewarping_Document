using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
// Specify all the using statements which give us the access to all the APIs that you'll need
using System;
using System.Threading.Tasks;
using Windows.AI.MachineLearning;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using System.Numerics.Tensors;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // All the required variable declaration
        private GeoTr_Seg_modelModel modelGen;
        private GeoTr_Seg_modelInput input = new GeoTr_Seg_modelInput();
        private GeoTr_Seg_modelOutput output;
        private StorageFile selectedStorageFile;

        private async Task LoadModel()
        {
            StorageFile modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/GeoTr_Seg_model.onnx"));
            modelGen = await GeoTr_Seg_modelModel.CreateFromStreamAsync(modelFile);
        }
        public MainPage()
        {
            this.InitializeComponent();
            LoadModel();
        }
        // A method to select an input image file
        private async Task<bool> getImage()
        {
            try
            {
                // Trigger file picker to select an image file
                FileOpenPicker fileOpenPicker = new FileOpenPicker();
                fileOpenPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                fileOpenPicker.FileTypeFilter.Add(".jpg");
                fileOpenPicker.FileTypeFilter.Add(".png");
                fileOpenPicker.ViewMode = PickerViewMode.Thumbnail;
                selectedStorageFile = await fileOpenPicker.PickSingleFileAsync();
                if (selectedStorageFile == null)
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        
        private void Output_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {

            if(!await getImage())
            {
                return;
            }
            await imageBind();
            //await evaluate();
            //extractResult();
            // Display the results  
            await displayResult();

        }
        private async Task imageBind()
        {
            UIPreviewImage.Source = null;
            try
            {
                SoftwareBitmap softwareBitmap;
                Bitmap image_bitmap;
                using (IRandomAccessStream stream = await selectedStorageFile.OpenAsync(FileAccessMode.Read))
                {
                    // Create the decoder from the stream 
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                    // Get the SoftwareBitmap representation of the file in BGRA8 format
                    softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                    softwareBitmap = await ResizeBitmap(softwareBitmap, 288, 288);
                }
                // Display the image
                SoftwareBitmapSource imageSource = new SoftwareBitmapSource();
                await imageSource.SetBitmapAsync(softwareBitmap);
                UIPreviewImage.Source = imageSource;
                // Encapsulate the image within a VideoFrame to be bound and evaluated
                using (var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream())
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    encoder.SetSoftwareBitmap(softwareBitmap);
                    await encoder.FlushAsync();
                    image_bitmap = new Bitmap(stream.AsStream());
                }
                Tensor<float> imageTensor = ConvertImageToFloatTensorUnsafe(image_bitmap);
                
                input.input = imageTensor;
                Console.WriteLine(input.input.ToString());
            }
            catch (Exception e)

            {
            }
        }
        private async Task evaluate()
        {
            output = await modelGen.EvaluateAsync(input);
        }
        private void extractResult()
        {
            // A method to extract output (result and a probability) from the "loss" output of the model 
            TensorFloat result = output.output;
        }
        private async Task displayResult()
        {
            

        }
        private async Task<SoftwareBitmap> ResizeBitmap(SoftwareBitmap softwareBitmap, uint width, uint height)
        {
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, stream);

                encoder.SetSoftwareBitmap(softwareBitmap);

                encoder.BitmapTransform.ScaledWidth = width;
                encoder.BitmapTransform.ScaledHeight = height;
                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.NearestNeighbor;

                await encoder.FlushAsync();

                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                return await decoder.GetSoftwareBitmapAsync(softwareBitmap.BitmapPixelFormat, softwareBitmap.BitmapAlphaMode);
            }
        }
        public Tensor<float> ConvertImageToFloatTensorUnsafe(Bitmap image)
        {
            // Create the Tensor with the appropiate dimensions  for the NN
            Tensor<float> data = new DenseTensor<float>(new[] { 1, image.Width, image.Height, 3 });

            BitmapData bmd = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            int PixelSize = 3;

            unsafe
            {
                for (int y = 0; y < bmd.Height; y++)
                {
                    // row is a pointer to a full row of data with each of its colors
                    byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride);
                    for (int x = 0; x < bmd.Width; x++)
                    {
                        // note the order of colors is BGR
                        data[0, y, x, 0] = row[x * PixelSize + 2] / (float)255.0;
                        data[0, y, x, 1] = row[x * PixelSize + 1] / (float)255.0;
                        data[0, y, x, 2] = row[x * PixelSize + 0] / (float)255.0;
                    }
                }

                image.UnlockBits(bmd);
            }
            return data;
        }
    }
}
