# Verifikasi 0.6.0 — Chronicles

Pemeriksaan dilakukan pada 25–26 Oktober 2026 menggunakan Unity 6000.3.20f1. Versi ini memperluas fondasi ilustrasi 0.5.0 menjadi artisan crafting, ekspedisi tiga anggota dan dunia dengan perkembangan yang tersimpan. Semua wajah manusia dibuat polos tanpa mata, hidung atau mulut, termasuk pekerja, warga, hero, potret NPC dan lawan humanoid.

## Cakupan yang dapat dimainkan

- Enam wilayah dan enam hero; party hingga tiga anggota dengan role skill berbeda.
- 108 resep, pilihan alloy/quench/rune dan equipment yang terlihat pada hero. Proyek artisan dibayar sekali dan dapat dilanjutkan setelah keluar atau memuat save. Pekerjaan otomatis yang sudah dibayar dapat diambil alih.
- 48 enemy biasa, 12 guardian dengan tiga fase dan counter yang memengaruhi hasil serangan. Rute membawa kesehatan antar ruang, menawarkan risiko dan satu camp, lalu membayar hasil sekali.
- Enam proyek restorasi hingga level 3, percakapan/trust warga, 12 legendary commission, challenge rank 0–3 serta legacy pilihan yang mempertahankan hero, cerita, kota dan koleksi.
- Enam tema musik, enam ambience, enam lapisan intensitas guardian dan 15 variasi impact material. Audio ini disusun secara prosedural; bukan rekaman orkestra atau produksi studio.
- Katalog JSON, compiler/validator konten, panduan pemain dan kit playtest manusia.

## Bukti pengujian

| Pemeriksaan paket native | Windows release | Emulator Android API 36 |
| --- | --- | --- |
| Alur artisan/party/saga, seluruh konten visual, teks besar | 3.039 lulus | 3.039 lulus |
| Regresi tampilan/navigasi | 1.211 lulus | 1.211 lulus |
| Alur dan progres lama | 793 lulus | 793 lulus |
| Ilustrasi dan seluruh arena lama | 2.019 lulus | 2.019 lulus |
| Aset dan penyimpanan Unity | 103 lulus | 103 lulus |

Logika C# lulus **242 pemeriksaan, 0 gagal**, dengan C# 7.3 dan warnings sebagai error. Tes mencakup migrasi save, batas konten, biaya/hasil sekali, tahapan artisan ketika offline, snapshot party, semua mekanik guardian, cabang/camp, pemuatan ulang, restorasi dan legacy tanpa kehilangan koleksi. Perjalanan baru dari kondisi awal biasa menyelesaikan enam wilayah dalam 389 keputusan dengan dua kekalahan, memakai produksi dan hadiah sebenarnya. Waktu simulasi 350,9 detik memakai input otomatis optimal; angka ini tidak mewakili durasi atau pengalaman pemain manusia.

Tes aset/konten/audio lulus **72 pemeriksaan, 0 gagal**. Suite logika dan aset yang sama juga lulus dari ekspor berkas staging, sehingga verifikasi tidak bergantung pada file sumber yang belum masuk Git. Dua puluh enam atlas diperiksa alpha, crop, pemisahan tetangga dan skala; 29 PNG pilihan cocok byte demi byte dengan keluaran imagegen asli. Twin blades mempertahankan kedua bilah yang terpisah. Validator menolak ID duplikat, baris hilang, referensi/effect/timing tidak valid dan kompilasi katalog yang tidak sinkron.

Probe native memakai save QA terpisah dan mengklik tombol melalui raycast UI. Bagian awal benar-benar menempa, memasang equipment, merekrut/mengatur party, memilih camp dan memenangkan rute tiga ruang. Bagian berikutnya memakai fixture render yang membuka konten untuk memeriksa semua hero, 108 ikon, 48 enemy dan 12 guardian; screenshot fixture bukan bukti keseimbangan atau progres yang diperoleh pemain. Teks 150%, layar ringkas, Reduced motion, kelanjutan masterwork dan cooldown Runebreak diperiksa. Filter dan skill menyesuaikan lebar pada teks besar supaya kata tidak terbelah.

Review independen mereproduksi dan memperbaiki validasi snapshot equipment, kelanjutan masterwork dari workshop, hitungan barrier Riposte, counter Focus milik Cinder Colossus serta unit kosong yang menyimpan ID senjata rusak. Tes gagal sebelum perbaikan dan lulus sesudahnya. Unity JsonUtility juga membuktikan pertempuran lama tetap aktif ketika field Saga baru tersimpan sebagai record kosong. Reviewer terakhir melaporkan tidak ada temuan terkonfirmasi yang tersisa.

## Paket dan batas pengujian

Build memakai konfigurasi release tanpa watermark development. Probe QA tersedia lewat argumen khusus dan tidak berjalan pada sesi biasa. Windows menggunakan Mono; Android menggunakan IL2CPP ARM64, minimum API 26, target API 36 dan version code 6. Ilustrasi Android diimpor ETC2 RGBA8, maksimum 2048.

| Paket | Ukuran | SHA-256 |
| --- | --- | --- |
| `ForgeEmpire.apk` | 103.362.109 bytes | `a75974abcd39618eb168dc8e0e7cb9ab61ef2277c2e01ef1637e4d5cae64d3ac` |
| `ForgeEmpire-Windows.zip` | 129.312.366 bytes | `3e07fc99aa9616d86c962fab0b886abd67f78c2f309fa0f0fd5d4f1b27305509` |

ZIP Windows diperiksa CRC dan kelengkapan 147 entri, termasuk executable, runtime dan panduan bermain terbaru. Screenshot native akhir diperiksa pada kedua target. HP fisik yang sebelumnya diizinkan untuk pemasangan tidak terhubung pada pemeriksaan akhir 26 Oktober; APK 0.6.0 telah dipasang dan diuji pada emulator, belum dipasang pada HP tersebut.

Belum ada sesi playtest manusia atau pengukuran retensi. Kualitas pacing jangka panjang dan performa pada ragam HP tetap memerlukan sesi nyata; gunakan `PLAYTEST-CHRONICLES.md`. Ini adalah perluasan game yang dapat dimainkan, bukan klaim skala produksi AAA. Angka pemeriksaan bukan skor kualitas visual atau kepuasan pemain.

Bukti mentah, screenshot, log, red/green review dan hash paket disimpan lokal di `QA/chronicles-20261025`. Cache, paket build dan data simpan tidak dimasukkan ke Git. Jalankan probe Android setelah Unity benar-benar keluar karena editor menutup ADB saat berhenti.
