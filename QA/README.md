# Keluaran pengujian lokal

Folder ini disediakan agar tes dan verifikasi build dapat menulis hasil setelah proyek di-clone. Hasil tes, screenshot, log, arsip dan profil simpan QA di dalamnya diabaikan oleh Git.

- `node tools/test-core.mjs local` menjalankan tes simulasi C#.
- `node tools/test-native-android.mjs progression <serial-emulator>` menguji alur APK pada emulator.
- `node tools/test-native-android.mjs regression <serial-emulator>` menguji regresi tampilan APK pada emulator.
- `node tools/test-native-android.mjs illustrated <serial-emulator>` menguji atlas, guardian, peta dan semua latar wilayah.

Ringkasan pengujian versi 0.5.0 terdapat di `docs/VERIFICATION-0.5.0.md`.
