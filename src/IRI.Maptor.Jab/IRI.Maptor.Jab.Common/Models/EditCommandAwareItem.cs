using System; 

using IRI.Maptor.Jab.Common.Assets.Commands;

namespace IRI.Maptor.Jab.Common.Models;

public class EditCommandAwareItem : Notifier
{
    public Action RequestRemoveAction { get; set; }

    public Action RequestAddAction { get; set; }

    public Action RequestEditAction { get; set; }


    public EditCommandAwareItem()
    {

    }

    public EditCommandAwareItem(Action removeAction, Action addAction, Action editAction)
    {
        this.RequestRemoveAction = removeAction;

        this.RequestAddAction = addAction;

        this.RequestEditAction = editAction;
    }

    private RelayCommand _removeCommand;

    public RelayCommand RemoveCommand
    {
        get
        {
            if (_removeCommand == null)
            {
                _removeCommand = new RelayCommand(param => this.RequestRemoveAction?.Invoke());
            }
            return _removeCommand;
        }
    }

    private RelayCommand _addCommand;

    public RelayCommand AddCommand
    {
        get
        {
            if (_addCommand == null)
            {
                _addCommand = new RelayCommand(param => this.RequestAddAction?.Invoke());
            }
            return _addCommand;
        }
    }

    private RelayCommand _editCommand;

    public RelayCommand EditCommand
    {
        get
        {
            if (_editCommand == null)
            {
                _editCommand = new RelayCommand(param => this.RequestEditAction?.Invoke());
            }
            return _editCommand;
        }
    }

    //public event EventHandler OnRemove;

    //public event EventHandler OnAdd;

    //public event EventHandler OnEdit;
}
