# Verifikasi 0.7.0 — Battle & Forge

Pemeriksaan dilakukan pada 26 Oktober 2026 dengan Unity 6000.3.20f1. Perubahan berfokus pada keterbacaan pertarungan, armor yang benar-benar dikenakan, dan proses creation dengan pilihan serta feedback yang sesuai hasil sebenarnya.

## Perubahan yang diverifikasi

- Kontak hero dijadwalkan bergantian setelah persiapan/pendekatan. Aksi lawan mengunci target, memberi waktu respons 1,2 detik untuk serangan biasa dan tiga detik untuk mekanik guardian. Komando skill memiliki jeda bersama 0,7 detik.
- Tactical pause menyimpan jam, cooldown dan serangan yang masih tertunda; workshop tetap maju. Break menghentikan lawan selama dua detik dan memberi tambahan 35% damage, lalu memiliki masa pemulihan.
- Armor menggunakan 18 atlas tubuh lengkap, enam hero × tiga keluarga siluet × 20 pose. Mail, leather dan robe mengikuti tubuh di setiap aksi; setiap resep dalam keluarga yang sama berbagi siluet. Ikon armor tidak lagi dibuat di atas hero. Semua wajah manusia tetap polos.
- Biaya alloy ditunjukkan sebelum bahan diambil. Preview power/guard memakai perhitungan barang akhir yang sama; riwayat tiga pukulan dan kualitasnya disimpan. Pilihan quench/rune menghasilkan efek sebenarnya. Equip/Keep mengikuti kondisi inventory dan ekspedisi yang sedang berjalan.
- Arena, bar kesehatan ringkas, skill dan pause tetap terlihat pada layar ringkas dengan teks 150%. Bench tetap terlihat saat shaping; timing, tombol pukul dan pilihan hasil berada pada kontrol tetap. Angka impact berdekatan memakai posisi terpisah tanpa kotak penutup.

## Logika dan aset

Suite C# lulus **271 pemeriksaan, 0 gagal**, memakai C# 7.3 dengan warnings sebagai error. Cakupannya termasuk target terkunci, serangan kelompok, semua 12 counter guardian, break dan pemulihan, event damage/heal sebenarnya, pembagian frame, migrasi jadwal lama, pause/reload pada kontak tertunda, hasil rute yang dibayar sekali, serta feedback/preview/biaya creation. Preview semua 36 kombinasi quality/alloy/temper yang diuji cocok dengan hasil sebenarnya. Review sumber independen juga menjalankan 271 pemeriksaan dan tidak menemukan masalah Critical/Important yang tersisa.

Fixture wilayah 1 dengan tiga anggota level 6 dan gear kualitas 1, menggunakan skill sebenarnya, mengukur **16,6 detik** untuk lawan biasa dan **31,3 detik** untuk guardian. Perjalanan Chronicles dari save baru mencapai enam wilayah dalam 341 keputusan, tanpa kekalahan, dengan 1.098,7 detik simulasi. Inputnya otomatis dan optimal; angka ini bukan durasi sesi manusia atau bukti retensi. Gear yang lebih kuat tetap dapat mempercepat pertempuran karena HP lawan tidak menyalin power pemain.

Suite aset/konten/audio lulus **91 pemeriksaan, 0 gagal**. Delapan belas PNG outfit cocok byte demi byte dengan keluaran imagegen pilihan dalam `ArtSource/worn-armor-final.json`. Analisis alpha memastikan 360 crop tidak saling mengambil gambar tetangga, memakai ukuran referensi pose yang konsisten dan memiliki detail sumber yang memadai. Generator analisis hanya menulis metadata. Semua atlas pilihan dilihat langsung; probe native menampilkan 18 galeri dengan 20 pose masing-masing.

Suite C# dan aset yang sama juga lulus dari ekspor berkas staging. Validator katalog menegaskan 108 resep, 48 enemy dan 12 guardian dengan identitas lama tetap utuh, sehingga verifikasi tidak bergantung pada sumber yang belum dimasukkan ke Git.

## Paket Windows

Release Windows final melewati **139 pemeriksaan aset dan penyimpanan Unity**. Probe native versi ini lulus **6.681 pemeriksaan**. Regresi tampilan/navigasi lulus **1.211**, progres lama **793**, dan ilustrasi/arena lama **2.019**. Probe mengklik kontrol sebenarnya, membayar bahan, menyelesaikan creation, memasang gear, merekrut party dan memenangkan rute. Fixture render berikutnya membuka konten untuk pemeriksaan visual; fixture tersebut tidak membuktikan progres yang diperoleh pemain.

Screenshot native memperlihatkan ancaman/target, pendekatan/kontak, pause, jurnal, seluruh keluarga armor, serta shaping dan hasil pada teks 150%. Pemeriksaan keterlihatan menggunakan arena yang sebenarnya; layer atmosfer/impact sengaja dimask di dalam arena. Uji juga memastikan angka impact aktif tidak saling menutupi. Review visual independen tidak menemukan masalah material pada bukti armor, wajah polos, arena, kontrol dan bench yang diperiksa.

ZIP Windows berisi 147 entri dengan CRC dan daftar berkas yang cocok, termasuk executable, runtime dan panduan 0.7.0. Ukuran **161.244.629 bytes**, SHA-256 `f0da89b4f7b45ac049faf54d29e921c890acdc48e90a2b08c8874ccb538dd1ce`.

## Paket Android dan perangkat

Release Android menggunakan IL2CPP ARM64, minimum API 26, target API 36, version name 0.7.0 dan version code 7. AAPT menegaskan identitas paket, activity peluncur dan arsitektur tersebut tanpa flag aplikasi debuggable. Build melewati 139 pemeriksaan aset/penyimpanan Unity.

| Probe Android pada emulator API 36 | Hasil |
| --- | --- |
| Battle, artisan, party, semua outfit dan teks besar | 6.681 lulus |
| Regresi tampilan/navigasi | 1.204 lulus |
| Progres lama | 793 lulus |
| Ilustrasi dan arena lama | 2.019 lulus |

Jumlah pemeriksaan tampilan mengikuti label aktif pada layout yang diperiksa. Screenshot Android untuk ancaman/target, arena ringkas 150%, shaping dan hasil diperiksa langsung. Build juga dibuka dalam sesi biasa tanpa argumen QA; onboarding terlihat setelah dialog konfirmasi fullscreen dan peringatan System UI pada emulator ditutup. Tidak ada perubahan kode game untuk menangani dialog emulator tersebut. Instalasi pertama pada emulator yang baru melakukan cold boot melampaui batas transfer 30 detik alat bantu; APK sebenarnya terpasang. Batas instalasi alat bantu diperpanjang menjadi 120 detik, lalu semua probe dijalankan ulang hingga lulus. Batas waktu tes gameplay dan semua asersi dipertahankan.

APK berukuran **135.340.965 bytes**, SHA-256 `9ff9705de20a8c734faf4aac2827d64c90658d7b60b537a9ad5a991fd73045e5`. HP fisik yang sebelumnya dipakai tidak terdeteksi pada pengecekan akhir; APK 0.7.0 belum dipasang pada HP tersebut. Pengujian emulator tidak diklaim sebagai pengukuran performa HP fisik.

## Batas bukti

Pemeriksaan otomatis dan screenshot tidak menggantikan playtest manusia, pengukuran retensi atau pengukuran performa pada beragam HP. Kecepatan fixture bergantung pada build party dan pemilihan skill. Armor memakai tiga keluarga siluet per hero, bukan 108 desain tubuh unik. Ilustrasi tetap berupa sprite 2D yang dianimasikan dengan pose dan tween; versi ini tidak mengklaim skala produksi AAA.

Build memakai konfigurasi release. Probe hanya berjalan lewat argumen QA khusus dan memakai save terpisah. Hasil mentah dan screenshot disimpan lokal di `QA/battle-forge-20261026`; paket build dan data simpan tidak dimasukkan ke Git. Panduan kontrol: [Battle & Forge](PLAYING-BATTLE-FORGE.md).
