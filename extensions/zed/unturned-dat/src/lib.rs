use zed_extension_api::{self as zed, LanguageServerId, Result };

struct UnturnedDataFileExtension {
    
}

impl zed::Extension for UnturnedDataFileExtension {
    fn new() -> Self {
        UnturnedDataFileExtension { }
    }

    fn language_server_command(
        &mut self,
        language_server_id: &LanguageServerId,
        worktree: &zed::Worktree,
    ) -> Result<zed::Command> {
        Ok(zed::Command {
            command: String::from("test"),
            args: Vec::new(),
            env: Vec::new(),
        })
    }
}

zed::register_extension!(UnturnedDataFileExtension);