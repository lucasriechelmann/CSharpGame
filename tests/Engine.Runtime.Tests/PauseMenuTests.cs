using Engine.Runtime.Menus;

namespace Engine.Runtime.Tests;

public class PauseMenuTests
{
    [Fact]
    public void Starts_on_resume()
    {
        var menu = new PauseMenu();

        Assert.Equal(PauseChoice.Resume, menu.Selected);
        Assert.Equal(0, menu.SelectedIndex);
    }

    [Fact]
    public void Lists_resume_restart_and_quit_in_order()
    {
        Assert.Equal(new[] { PauseChoice.Resume, PauseChoice.Restart, PauseChoice.Quit }, new PauseMenu().Choices);
    }

    [Fact]
    public void Moving_down_walks_the_list()
    {
        var menu = new PauseMenu();

        menu.Move(1);
        Assert.Equal(PauseChoice.Restart, menu.Selected);
        menu.Move(1);
        Assert.Equal(PauseChoice.Quit, menu.Selected);
    }

    [Fact]
    public void Moving_down_from_the_last_entry_wraps_to_the_first()
    {
        var menu = new PauseMenu();
        menu.Select(PauseChoice.Quit);

        menu.Move(1);

        Assert.Equal(PauseChoice.Resume, menu.Selected);
    }

    [Fact]
    public void Moving_up_from_the_first_entry_wraps_to_the_last()
    {
        var menu = new PauseMenu();

        menu.Move(-1);

        Assert.Equal(PauseChoice.Quit, menu.Selected);
    }

    [Fact]
    public void Select_highlights_the_given_choice()
    {
        var menu = new PauseMenu();

        menu.Select(PauseChoice.Restart);

        Assert.Equal(1, menu.SelectedIndex);
    }

    [Fact]
    public void Reset_returns_to_resume()
    {
        var menu = new PauseMenu();
        menu.Select(PauseChoice.Quit);

        menu.Reset();

        Assert.Equal(PauseChoice.Resume, menu.Selected);
    }

    [Theory]
    [InlineData(PauseChoice.Resume, "Resume")]
    [InlineData(PauseChoice.Restart, "Restart run")]
    [InlineData(PauseChoice.Quit, "Quit")]
    public void Every_choice_has_a_label(PauseChoice choice, string expected)
    {
        Assert.Equal(expected, PauseMenu.Label(choice));
    }

    [Fact]
    public void Unknown_choice_has_no_label()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PauseMenu.Label((PauseChoice)42));
    }
}
