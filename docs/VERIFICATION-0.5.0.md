# Verifikasi 0.5.0 — illustrated world

Paket akhir Windows dan Android diuji pada **25 Oktober 2026** dengan Unity **6000.3.20f1**. Arah visual dan prompt produksi dicatat di `ArtSource/illustrated-world-generation.md`; audit sebelum perubahan berada di `DESIGN-IS-2026-10-25`.

## Hasil perubahan

Semua 20 equipment, lima enemy biasa, lima guardian, peta dan lima latar wilayah memakai artwork baru. Setiap enemy/guardian memiliki empat pose. Seluruh pemakai equipment, termasuk rak dan barang yang bergerak dari penempa, menggunakan frame baru. Bayangan yang menapak, gerak serang dan pemulihan, indikator serangan lembut, serta ikon tinta melengkapi tampilannya.

Peta menampilkan semua nama wilayah, tujuan aktif, status pemulihan dan preview wilayah terkunci. Pembukaan wilayah mengikuti klaim hadiah cerita, termasuk pembedaan boss saat ini, pengulangan boss dan finale. Penjelasan elemen senjata/armor, penerimaan pesanan dan reservasi barang disesuaikan dengan perilaku simulasi.

## Paket akhir

| Pemeriksaan | Windows | Emulator Android API 36 |
| --- | --- | --- |
| Atlas, item, peta, guardian, seluruh arena, besar/compact | 1.177 lulus | 1.177 lulus |
| Alur gameplay dan progres | 787 lulus | 787 lulus |
| Regresi tampilan/navigasi | 875 lulus | 875 lulus |
| Verifikasi aset dan penyimpanan Unity | 58 lulus | 58 lulus |

- Logika C#: **132 lulus, 0 gagal**. Lima bab dari bekal awal tetap selesai dalam 319 tindakan simulasi tanpa kekalahan.
- Tes aset: **31 lulus, 0 gagal**. Termasuk 20 frame per atlas, urutan resep, alpha asli, kesesuaian hash, crop yang memuat seluruh siluet opaque, dan pemisahan frame tetangga.
- Pengujian native memakai profil QA terpisah; simulasi di dalam render fixture dibekukan agar pose/gelombang dapat diperiksa. Angka HP pada screenshot fixture bukan tolok ukur keseimbangan. Tes alur dan core terpisah menjalankan progres/battle sebenarnya.
- Screenshot native Windows dan Android diperiksa, termasuk peta, perlengkapan, guardian dan teks besar pada layar ringkas.
- Satu temuan material review independen (ujung senjata terpotong oleh crop) diperbaiki dengan regenerasi atlas berjarak lebih besar. Assertion baru terlihat gagal pada pilihan lama lalu lulus pada hasil akhir. Tidak ada temuan material lain dari review sumber.
- Android memakai ARM64, minimum API 26, target API 36, code 5. Import setting ilustrasi Android menggunakan ETC2 RGBA8 dengan dimensi maksimum 2048. Profiler native melaporkan sekitar 56,6 MB untuk sembilan tekstur baru pada kedua target; angka ini tidak membuktikan penghematan GPU atau performa pada seluruh HP.
- ZIP Windows divalidasi dengan 236 entri, termasuk executable, runtime dan panduan. APK akhir berukuran 76.671.150 bytes; ZIP berukuran 105.737.420 bytes. Hasil build adalah development build dengan probe yang aktif hanya lewat argumen QA.

HP yang dipakai memasang 0.4.0 tidak terhubung saat pemeriksaan akhir 0.5.0, sehingga pembaruan fisik belum dilakukan. Pengujian lengkap Android di atas dilakukan pada emulator 1080 × 2400.

Bukti mentah, screenshot, log dan hash paket disimpan lokal di `QA/illustrated-20261025`. Cache, paket build dan data simpan tidak dimasukkan ke Git.
