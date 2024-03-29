function op_div_cancel_form()
{
    if ( confirm('Are you sure you want to cancel these changes?\n\n'))
    {
        var actionfield = document.getElementById('action_requested');
        actionfield.value = 'cancel';

        // Perform post back
        document.itemNavForm.submit();
    }
    return false;
}

function op_div_save_form()
{
        var actionfield = document.getElementById('action_requested');
        actionfield.value = 'save';

        // Perform post back
        document.itemNavForm.submit();
}

function get_structure()
{
    var returnValue = '';

    alert('build structure string');

    var test = $("#oer_div_container").children(".oer_div_outer");
    alert(test.length);


    var counter = 1;
    $("#oer_div_container").children(".oer_div_outer").each(function() {
        alert('doing stuff');
        var outer_title = this.children(".oer_div_outer_title").first().html;
        alert(outer_title);
        returnValue = returnValue + '|' + outer_title;
        counter++;
    });

    return returnValue;
}

function edit_chapter_keypress(index, name, type, isMozilla)
{
    var typeField = document.getElementById('form_chapter_type');
    typeField.value = type;

    var titleField = document.getElementById('form_chapter_title');
    titleField.value = name;

    // Set the hidden index field (may not be used though)
    var hiddenfield = document.getElementById('action_index');
    hiddenfield.value = index;

    popup_keypress_focus('form_new_chapter', 'form_chapter_title', '" + isMozilla.ToString() + "' );

    var actionfield = document.getElementById('action_requested');
    actionfield.value = 'edit_chapter';

    // Return false to prevent an immediate return trip to the server
    return false;
}

function edit_chapter(index, name, type)
{
    var typeField = document.getElementById('form_chapter_type');
    typeField.value = type;

    var titleField = document.getElementById('form_chapter_title');
    titleField.value = name;

    // Set the hidden index field (may not be used though)
    var hiddenfield = document.getElementById('action_index');
    hiddenfield.value = index;

    var actionfield = document.getElementById('action_requested');
    actionfield.value = 'edit_chapter';

    popup_focus('form_new_chapter', 'form_chapter_title');

    // Return false to prevent an immediate return trip to the server
    return false;
}

function show_chapter_form_keypress(index, isMozilla)
{
    // Set the hidden index field (may not be used though)
    var hiddenfield = document.getElementById('action_index');
    hiddenfield.value = index;

    // Set the hidden value based on the user request
    var actionfield = document.getElementById('action_requested');
    actionfield.value = 'new_chapter';

    popup_keypress_focus('form_new_chapter', 'form_chapter_title', '" + isMozilla.ToString() + "' );

    // Return false to prevent an immediate return trip to the server
    return false;
}

function show_chapter_form(index)
{
    // Set the hidden index field (may not be used though)
    var hiddenfield = document.getElementById('action_index');
    hiddenfield.value = index;

    // Set the hidden value based on the user request
    var actionfield = document.getElementById('action_requested');
    actionfield.value = 'new_chapter';

    popup_focus('form_new_chapter', 'form_chapter_title' );

    // Return false to prevent an immediate return trip to the server
    return false;
}


function cancel_new_chapter_form()
{
    // Close the associated form
    popdown( 'form_new_chapter' );    
        
    // Return false to prevent a return trip to the server
    return false;
}

function save_new_chapter_form()
{
    // Close the associated form
    popdown( 'form_new_chapter' );    
        
    // Get the name of this chapter
    var titlefield = document.getElementById('form_chapter_title');
    var title = titlefield.value;

    // Get the type of this chapter
    var typefield = document.getElementById('form_chapter_type');
    if ( title.length <= 0 )
    {
        title = typefield.value;
    }

    // Set the hidden value to have the new chapter name
    var hiddennamefield = document.getElementById('action_value');
    hiddennamefield.value = title;
    
	// Perform post back
    document.itemNavForm.submit();
    return false;
}


function show_division_form_keypress(index, isMozilla)
{
    // Set the hidden index field (may not be used though)
    var hiddenfield = document.getElementById('action_index');
    hiddenfield.value = index;

    popup_keypress_focus('form_new_division', 'form_division_title', '" + isMozilla.ToString() + "' );

    // Return false to prevent an immediate return trip to the server
    return false;
}

function show_division_form(index)
{
    // Set the hidden index field (may not be used though)
    var hiddenfield = document.getElementById('action_index');
    hiddenfield.value = index;

    popup_focus('form_new_division', 'form_division_title' );

    // Return false to prevent an immediate return trip to the server
    return false;
}


function cancel_new_division_form()
{
    // Close the associated form
    popdown( 'form_new_division' );    
        
    // Return false to prevent a return trip to the server
    return false;
}

function save_new_division_form()
{
    // Close the associated form
    popdown( 'form_new_division' );    
        
    // Set the hidden value based on the user request
    var actionfield = document.getElementById('action_requested');
    actionfield.value = 'new_division';

    // Get the name of this division
    var titlefield = document.getElementById('form_division_title');
    var title = titlefield.value;

    // Set the hidden value to have the new chapter name
    var hiddennamefield = document.getElementById('action_value');
    hiddennamefield.value = title;
    
    // Perform post back
    document.itemNavForm.submit();
    return false;
}

function op_handle_title_keypress(e)
{
    if(e.keyCode === 13) {
        e.preventDefault(); // Ensure it is only this code that runs

        // Get the name of this chapter
        var titlefield = document.getElementById('form_chapter_title');
        var title = titlefield.value;

        if ( title.length > 0 )
        {
            return save_new_chapter_form();        
        }
    }
}

function op_handle_divtitle_keypress(e)
{
    if(e.keyCode === 13) {
        e.preventDefault(); // Ensure it is only this code that runs
        // Get the name of this chapter
        var titlefield = document.getElementById('form_division_title');
        var title = titlefield.value;

        if ( title.length > 0 )
        {
            return save_new_division_form();        
        }       
    }
}


function delete_chapter(index, name)
{
    if ( confirm('Are you sure you want to delete this chapter?\n\nTitle: ' + name ))
    {
        // Set the hidden index field (may not be used though)
        var hiddenfield = document.getElementById('action_index');
        hiddenfield.value = index;

        var actionfield = document.getElementById('action_requested');
        actionfield.value = 'delete_chapter';

        // Perform post back
        document.itemNavForm.submit();
    }
    return false;
}

function delete_division(cindex, dindex, name)
{
    if ( confirm('Are you sure you want to delete this division?\n\nTitle: ' + name ))
    {
        // Set the hidden index field (may not be used though)
        var hiddenfield = document.getElementById('action_index');
        hiddenfield.value = cindex + '|' + dindex;

        var actionfield = document.getElementById('action_requested');
        actionfield.value = 'delete_division';

        // Perform post back
        document.itemNavForm.submit();
    }
    return false;
}