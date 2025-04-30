echo starting game process...

#dump obj debug files
rm -rf obj/Debug

dotnet run && sh clean_mod_files.sh

echo process complete.