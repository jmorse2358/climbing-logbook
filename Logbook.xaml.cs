using System.Diagnostics;

namespace Climbing_Logbook;
public partial class Logbook : ContentPage
{

    // text file names & paths
    private const string tag_file = "tag_list.txt";
    private const string feature_file = "feature_list_logbook.txt";

    // lists
    private List<DatabaseHelper.climb_entry> stored_entries = new List<DatabaseHelper.climb_entry>();
    private List<string> tag_list = new List<string>();

    public Logbook()
    {
        InitializeComponent();
        main();
    }

    // main procedure; calls other things
    private void main()
    {
        load_tags();
        read_entries();
        display_climbs();
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        read_entries();  // ensure the logbook updates every time the page is shown
        refresh_logbook();
    }
    private async void read_entries()
    {
        // leftover data cleared
        stored_entries.Clear();

        stored_entries = DatabaseHelper.get_climbs();
        MainThread.InvokeOnMainThreadAsync(display_climbs);
    }
    private void display_climbs()
    {
        if (log_list == null) return; // double check it exists just in case

        // clears leftovers
        log_list.Children.Clear();

        // iterates over stored_entries, adds info from that into labels that are then appended to the display
        foreach (var climb in stored_entries)
        {

            Border border = new Border
            {
                Stroke = Colors.Black,
                StrokeThickness = 1,
                Margin = 5,
                Padding = 5,
                BackgroundColor = Colors.DarkGray
            };

            // uses the c# version of f-strings to concatenate all the data together
            VerticalStackLayout panel = new VerticalStackLayout();
            panel.Children.Add(new Label { Text = $"{climb.Name} • {climb.Attempts} • {climb.DateAdded}", FontAttributes = FontAttributes.Bold });
            panel.Children.Add(new Label { Text = $"{climb.ActualGrade}  {climb.Score} pts" });
            panel.Children.Add(new Label { Text = $"Tags: {decode_tags(climb.TagCheckSum.ToString())}" }); // <== Decode tags call here
            border.Content = panel;

            HorizontalStackLayout buttonPanel = new HorizontalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new ImageButton
                    {
                        Source = "edit.png",
                        WidthRequest = 50,
                        HeightRequest = 50,
                        Command = new Command(() => edit_climb(climb))
                    },
                    new ImageButton
                    {
                        Source = "delete.png",
                        WidthRequest = 50,
                        HeightRequest = 50,
                        Command = new Command(async () => await delete_climb(climb.Id))
                    }
                }
            };

            panel.Children.Add(buttonPanel);
            border.Content = panel;
            log_list.Children.Add(border); // all added here
        }
    }
    private void load_tags()
    {
        try
        {
            // moves the file to the readable area of memory
            using var stream = FileSystem.OpenAppPackageFileAsync(tag_file).Result;
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                var tag = reader.ReadLine();
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    tag_list.Add(tag);
                }
            }
        }
        catch (Exception ex)
        {
            // suitable error message
            DisplayAlert("Error", "Failed to load tags.", "OK");
        }
    }
    public string decode_tags(string tag_checksum)
    {
        if (string.IsNullOrEmpty(tag_checksum) || tag_checksum == "00000000000")
        {
            return "None";
        }

        List<string> activeTags = new List<string>();

        // ensure we don't exceed the length of either array
        int checksumLength = Math.Min(tag_checksum.Length, tag_list.Count);

        // for each '1' in the checksum, add the corresponding tag
        for (int i = 0; i < checksumLength; i++)
        {
            if (i < tag_checksum.Length && tag_checksum[i] == '1' && i < tag_list.Count)
            {
                activeTags.Add(tag_list[i]);
            }
        }

        // join the tags with commas in this funky one liner    
        return activeTags.Count > 0 ? string.Join(", ", activeTags) : "None";
    }
    private void display_info(object sender, EventArgs e)
    {
        refresh_logbook();
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(feature_file).Result;
            using var reader = new StreamReader(stream);
            DisplayAlert("Key Features", reader.ReadToEnd(), "OK");
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", "Feature list not found.", "OK");
        }
    }
    private async Task delete_climb(int id)
    {
        // double check to make sure the user wants to remove the climb
        bool confirm = await DisplayAlert("Delete Climb", "Are you sure you want to delete this climb?", "Yes", "No");
        if (confirm)
        {
            // calls the databse helper procedure
            DatabaseHelper.delete_entry(id);

            // refreshs the logbook
            refresh_logbook();
        }
    }
    private async void edit_climb(DatabaseHelper.climb_entry climb)
    {
        // makes sure the clibm exists
        if (climb != null)
        {
            // opens a new add climb logbook page, this time with instructions to fill in all the fields with the values from the databse
            await Navigation.PushAsync(new AddClimbLogbook(refresh_logbook, climb));
        }
    }
    private async void add_climb_click(object sender, EventArgs e)
    {
        // how much much simpler & the AddClimbLogbook page exists
        refresh_logbook();
        await Navigation.PushAsync(new AddClimbLogbook(refresh_logbook));

    }
    private async void refresh_logbook()
    {
        read_entries();  // Ensure the logbook updates right after adding a climb
    }
}