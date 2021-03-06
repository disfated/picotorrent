#pragma once

#include <picotorrent/client/ui/dialogs/dialog_base.hpp>
#include <picotorrent/core/signals/signal.hpp>

#include <functional>
#include <memory>
#include <string>
#include <vector>

namespace picotorrent
{
namespace client
{
namespace ui
{
namespace controls
{
    class list_view;
}
namespace dialogs
{
    class add_torrent_dialog : public dialog_base
    {
    public:
        add_torrent_dialog();
        ~add_torrent_dialog();

        void add_torrent(const std::string &name);
        void add_torrent_file(const std::string &name, int64_t size, const std::string &priority);
        void clear_torrent_files();
        void enable_files();
        void disable_files();
        int get_selected_torrent();
        core::signals::signal_connector<void, void>& on_update_storage_mode();
        void set_file_priority(int index, const std::string &prio);
        void set_init_callback(const std::function<void()> &callback);
        void set_change_callback(const std::function<void(int)> &callback);
        void set_edit_save_path_callback(const std::function<void()> &callback);
        void set_file_context_menu_callback(const std::function<void(const std::vector<int> &files)> &callback);
        void set_save_path(const std::string &path);
        void set_selected_item(int item);
        void set_size(int64_t size);
        void set_size(const std::string &friendly_size);
        bool use_full_allocation();

    protected:
        BOOL on_command(int, WPARAM, LPARAM);
        BOOL on_init_dialog();
        BOOL on_notify(LPARAM);

        std::string on_list_display(const std::pair<int, int> &p);

    private:
        struct file_item;

        std::function<void()> init_cb_;
        std::function<void(int)> change_cb_;
        std::function<void(const std::vector<int> &files)> files_context_cb_;
        std::function<void()> save_path_cb_;
        std::shared_ptr<controls::list_view> files_;
        std::vector<file_item> items_;

        core::signals::signal<void, void> on_update_storage_mode_;

        HWND combo_;
        HWND save_path_;
        HWND size_;
    };
}
}
}
}
