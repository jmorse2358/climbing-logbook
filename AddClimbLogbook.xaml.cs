using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Maui.Controls;
using SQLite;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Climbing_Logbook;

public partial class AddClimbLogbook : ContentPage
{
    private readonly Action refreshCallback;

    // Database file path (s)
    private const string database_path = "logbook.db";

    // text files
    private const string feature_file = "feature_list_addclimblogbook.txt";
    private const string grade_file = "grades.txt";
    private const string rock_file = "rocktype_list.txt";
    private const string tag_file = "tag_list.txt";

    // misc initializations
    private string selected_climb_type = "";

    // List & dictionary initializations
    private List<string> grade_list = new List<string>();
    private List<string> tag_list = new List<string>();
    private List<string> rock_type_list = new List<string>();

    private Dictionary<string, double> tag_multipliers = new Dictionary<string, double>
    {
        { "Overhang", 1.5 }, { "Crimpy", 1.45 }, { "Balance-Heavy", 1.05 },
        { "Dyno", 1.75 }, { "Red-Point", -300 }, { "Fall", -450 }
    };
    public AddClimbLogbook(Action refreshCallback, DatabaseHelper.climb_entry climbToEdit = null)
    {
        InitializeComponent();
        this.refreshCallback = refreshCallback; // one of many efforts to make the updates work

        // loads all the data from the text files
        load_grades();
        load_tags();
        load_rock_types();

        // if the page has be created from the add new climb button this will not apear
        // this section of code will only run when a climb is edited
        if (climbToEdit != null)
        {
            var logbook = new Logbook();
            // resets all the data from the entry
            entry_name.Text = climbToEdit.Name; // required
            entry_notes.Text = climbToEdit.Notes;
            picker_actual_grade.SelectedItem = climbToEdit.ActualGrade; // required
            picker_recommended_grade.SelectedItem = climbToEdit.RecommendGrade;
            entry_tag_search.Text = logbook.decode_tags(climbToEdit.TagCheckSum.ToString());
            list_tags.SelectedItems = logbook.decode_tags(climbToEdit.TagCheckSum.ToString()).Split(", ");
            entry_attempts.Text = climbToEdit.Attempts.ToString(); // required


            if (climbToEdit.Outdoor == 1)
            {
                radio_outdoor.IsChecked = true;
                radio_indoor.IsChecked = false;
            }
            else
            {
                radio_outdoor.IsChecked = false;
                radio_indoor.IsChecked = true;
            }

            btn_sport.Opacity = 1.0;
            //btn_trad.Opacity = 0.5;
            //btn_bouldering.Opacity = 0.5;
            //btn_toprope.Opacity = 0.5;

            btn_sport.Opacity = 0.5;
            btn_trad.Opacity = 0.5;
            btn_bouldering.Opacity = 0.5;
            btn_toprope.Opacity = 0.5;

            if (climbToEdit.ClimbType == "sport") { btn_sport.Opacity = 1.0; selected_climb_type = "sport"; }
            else if (climbToEdit.ClimbType == "trad") { btn_trad.Opacity = 1.0; selected_climb_type = "trad"; }
            else if (climbToEdit.ClimbType == "bouldering") { btn_bouldering.Opacity = 1.0; selected_climb_type = "bouldering"; }
            else if (climbToEdit.ClimbType == "toprope") { btn_toprope.Opacity = 1.0; selected_climb_type = "toprope"; }

            Debug.Write($"{selected_climb_type}");


            // do the indoor/outdoor stuff
            picker_rock_type.SelectedItem = climbToEdit.RockType;
            slider_rock_quality.Value = climbToEdit.RockQuality;

            if (climbToEdit.ClimbType == "sport")
            {
                btn_sport.IsEnabled = true;
            }
            else if (climbToEdit.ClimbType == "bouldering")
            {
                btn_bouldering.IsEnabled = true;
            }
            else if (climbToEdit.ClimbType == "trad")
            {
                btn_trad.IsEnabled = true;
            }
            else
            {
                btn_toprope.IsEnabled = true;
            }

            // some validation in case something went wrong with the strings in the database
            if (DateTime.TryParse(climbToEdit.DateAdded, out DateTime parsedDate))
            {
                picker_date.Date = parsedDate;
            }
            else
            {
                picker_date.Date = DateTime.Now;
            }


            // using the FindByName function due to some issue with file linkage
            var save_button = this.FindByName<ImageButton>("btn_save");
            var add_climb_button = this.FindByName<ImageButton>("btn_add");

            // makes the add climb button invisble when editing & vise verse in the else statment
            add_climb_button.IsVisible = false;
            // gives the save button a function to call when clicked
            save_button.Clicked += (s, e) => update_existing_entry(climbToEdit.Id);
        }
        else
        {
            var save_button = this.FindByName<ImageButton>("btn_save");
            save_button.IsVisible = false;
        }
    }
    private void update_existing_entry(int id)
    {
        // does some validation, same as before, to make sure required fields are filled out
        if (string.IsNullOrWhiteSpace(entry_name.Text) || picker_actual_grade.SelectedItem == null || entry_attempts.Text == null)
        {
            // suitable error message
            DisplayAlert("Error", "Please fill in all required fields.", "OK");
            return;
        }

        // required fields
        string name = entry_name.Text;
        string actual_grade = picker_actual_grade.SelectedItem.ToString();
        int attempts = int.TryParse(entry_attempts.Text, out int parsed_attempts) ? parsed_attempts : 1;

        // non-required field
        string notes = entry_notes.Text ?? "";
        string recommended_grade = picker_recommended_grade.SelectedItem?.ToString() ?? actual_grade;
        bool outdoors = radio_outdoor.IsChecked;
        string rock_type = outdoors ? picker_rock_type.SelectedItem?.ToString() ?? "" : "";
        //int rock_quality = outdoors ? (int)slider_rock_quality.Value : 0;
        double rock_quality = slider_rock_quality.Value;
        string climb_type = selected_climb_type;
        //Debug.Write($"selected climb type: {climb_type}");
        DateTime date_added = picker_date.Date;
        DateTime date_modified = DateTime.Now;
        string tag_checksum = calculate_tag_checksum(entry_tag_search);
        int score = calculate_score(actual_grade, tag_checksum, attempts);


        // calls the database helper function to edit the database
        DatabaseHelper.edit_entry(id, name, notes, actual_grade, recommended_grade, tag_checksum, attempts, outdoors, rock_type, rock_quality, climb_type, score, date_added, date_modified);

        // suitable success message
        DisplayAlert("Success", "Climb updated successfully!", "OK");

        // refreshs the mainpage so that the edited attributes show
        refreshCallback?.Invoke();

        // navigates back to logbook
        Navigation.PopAsync();
    }
    private void load_grades()
    {
        // clears some existing data structures
        picker_actual_grade.Items.Clear();
        picker_recommended_grade.Items.Clear();

        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(grade_file).Result;
            using var reader = new StreamReader(stream);
            string content = reader.ReadToEnd();
            string[] grades = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var grade in grades)
            {
                picker_actual_grade.Items.Add(grade);
                picker_recommended_grade.Items.Add(grade);
            }
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", "Could not load grade file", "Ok");
        }
    }
    private void load_tags()
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(tag_file).Result;
            using var reader = new StreamReader(stream);
            string content = reader.ReadToEnd();
            tag_list = new List<string>(content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

        }
        catch (Exception ex)
        {
            DisplayAlert("Error", "Could not load tag file", "Ok");
        }
    }
    private void load_rock_types()
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(rock_file).Result;
            using var reader = new StreamReader(stream);
            string content = reader.ReadToEnd();
            rock_type_list = new List<string>(content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            picker_rock_type.ItemsSource = rock_type_list;
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", "Could not load rock type file", "Ok");
        }
    }
    private void climb_location_changed(object sender, CheckedChangedEventArgs e)
    {
        // switches around the radio buttons to ensure only one is clicked
        bool isIndoor = radio_indoor.IsChecked;
        picker_rock_type.IsEnabled = !isIndoor;
        slider_rock_quality.IsEnabled = !isIndoor;
    }
    private void climb_type_selected(object sender, EventArgs e)
    {
        // defines the selected button as the one calling the procedure
        ImageButton selected_button = sender as ImageButton;
        if (selected_button != null)
        {
            // sets the opacity of all non-selected buttons to half
            btn_sport.Opacity = 0.5;
            btn_trad.Opacity = 0.5;
            btn_bouldering.Opacity = 0.5;
            btn_toprope.Opacity = 0.5;

            // sets the opacity of the selected button to a full
            selected_button.Opacity = 1.0;

            // uses the file names to determine the selected climbing type, got to watch out for this in the future if other climbing types are added
            selected_climb_type = selected_button.Source.ToString().Replace(".png", "").Replace("File: ", "");

        }
    }
    private void filter_tags(object sender, TextChangedEventArgs e)
    {
        // this will filter tags whenever the xaml element of a similar name is altered
        string query = entry_tag_search.Text.ToLower();
        list_tags.ItemsSource = string.IsNullOrWhiteSpace(query)
        ? tag_list
        : tag_list.FindAll(tag => tag.ToLower().Contains(query));
    }
    private void create_new_entry(object sender, EventArgs e)
    {
        // makes sure the user has filled in the required fields (name, acctual actual grade & attempts)
        if (string.IsNullOrWhiteSpace(entry_name.Text) || picker_actual_grade.SelectedItem == null || entry_attempts.Text == null)
        {
            DisplayAlert("Error", "Please fill in all required fields.", "OK");
            return;
        }

        // wall of text to define the local parameters to be passed into the procedure defined in the database helper file
        string name = entry_name.Text;
        string notes = entry_notes.Text ?? "";
        string actual_grade = picker_actual_grade.SelectedItem.ToString();
        string recommended_grade = picker_recommended_grade.SelectedItem?.ToString() ?? actual_grade;
        string tag_checksum = calculate_tag_checksum(entry_tag_search);
        int attempts = int.TryParse(entry_attempts.Text, out int parsed_attempts) ? parsed_attempts : 1;
        bool outdoors = radio_outdoor.IsChecked;
        string rock_type = outdoors ? picker_rock_type.SelectedItem?.ToString() ?? "" : "";
        double rock_quality = slider_rock_quality.Value;
        //int Rock_quality = outdoors ? (int)slider_rock_quality.Value : 0;
        string climb_type = selected_climb_type;
        int score = calculate_score(actual_grade, tag_checksum, attempts);
        DateTime date_added = picker_date.Date;
        DateTime date_modified = DateTime.Now;

        // calls the aforementioned file, using all relevant parameters
        DatabaseHelper.add_climb(name, notes, actual_grade, recommended_grade, tag_checksum, attempts, outdoors, rock_type, rock_quality, climb_type, score, date_added, date_modified);

        // notify the user to the success
        DisplayAlert("Success", "Climb added successfully!", "OK");

        refreshCallback?.Invoke();

        // returns the user to the logbook page
        Navigation.PopAsync();
    }
    public string calculate_tag_checksum(Entry entry_tag_search)
    {
        // defines the checksum to be added to & the tag list to be appended to
        var checksum = "";
        var tags = new List<string>();
        // appends stuff out of the list_tag xaml element
        foreach (var tag in list_tags.SelectedItems)
        {
            tags.Add(tag.ToString());
        }
        // compars this with the tag_list & cretes the checksum accordingly
        foreach (var tag in tag_list)
        {
            if (tags.Contains(tag)) { checksum += "1"; }
            else { checksum += "0"; }
        }
        return checksum;
    }
    public static int calculate_score(string grade, string tag_checksum, int attempts)
    {
        const int baseValue = 1000;
        double score = baseValue;

        // convert grade to numeric value
        int gradeValue = 0;
        if (grade.Length > 0 && char.IsDigit(grade[0]))
        {
            gradeValue = (grade[0] - '0') * 100;
            if (grade.Length > 1)
            {
                switch (grade[1])
                {
                    case 'a': gradeValue += 10; break;
                    case 'b': gradeValue += 20; break;
                    case 'c': gradeValue += 30; break;
                }
            }
            if (grade.Length > 2 && grade[2] == '+')
            {
                gradeValue += 5;
            }
        }
        score += gradeValue;

        // apply tag multipliers and deductions
        double tagMultiplier = 1.0;
        int tagReduction = 0;

        var logbook = new Logbook();
        string tagsString = logbook.decode_tags(tag_checksum);
        List<string> tags = tagsString.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).ToList();

        foreach (var tag in tags)
        {
            switch (tag.ToLower())
            {
                case "overhang":
                    tagMultiplier *= 1.5;
                    break;
                case "crimpy":
                    tagMultiplier *= 1.45;
                    break;
                case "balance-heavy":
                    tagMultiplier *= 1.05;
                    break;
                case "dyno":
                    tagMultiplier *= 1.75;
                    break;
                case "red-point":
                    tagReduction += 300;
                    break;
                case "fall":
                    tagReduction += 450;
                    break;
            }
        }

        // Apply attempt multiplier: 1 / (0.1 * attempts + 0.9) to decay quickly after the first few attempts
        double attemptMultiplier = 1.0 / (0.1 * attempts + 0.9);

        // Calculate final score
        score = (score * tagMultiplier * attemptMultiplier) - tagReduction;

        // Ensure score doesn't drop below 0
        return Math.Max(0, (int)Math.Round(score));
    }
    private int get_grade_value(string grade)
    {
        if (string.IsNullOrWhiteSpace(grade) || !grade_list.Contains(grade))
        {
            return 0;
        }
        return grade_list.IndexOf(grade);
    }
    private void display_info(object sender, EventArgs e)
    {
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
}