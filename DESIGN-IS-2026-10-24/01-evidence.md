# Bukti gabungan audit

Audit sumber dan screenshot, 24 Oktober 2026. Tidak ada perubahan implementasi atau save. Angka pada rancangan usulan bukan pengukuran pengguna.

## E1 — Typography and scale

- `Game/Assets/ForgeEmpire/Runtime/WorkshopView.cs:61,76-82`: root memakai lebar 1080 dan skala `safe.width/1080f`. Ini penyesuaian terhadap lebar pixel, bukan ukuran teks fisik yang diuji di perangkat.
- `WorkshopView.cs:114,121-122,204-206,223-229`: nilai resource 30; objective 29/23; panel tindakan 33/23/22; label navigasi 21, semuanya dalam unit rancangan root. Pemeriksaan semua helper label/tombol menemukan 16 ukuran berbeda: `[20,21,22,23,24,25,26,27,28,29,30,31,32,33,48,52]`.
- Contoh normalisasi ke lebar tampilan 360: label navigasi `21 × 360/1080 = 7`; detail objective `23 × 360/1080 = 7,67`; nilai resource `30 × 360/1080 = 10`. Ini perbandingan proporsi, **bukan hasil pengukuran sp/dp atau font fisik Android**.
- `Game/Assets/ForgeEmpire/Runtime/ForgeUI.cs:41-48`: font LegacyRuntime.ttf, wrap horizontal dan truncate vertikal; ukuran dan tinggi kotak teks ditentukan eksplisit.
- Screenshot primer: `QA/campaign-android-initial-install-workshop.png`, teks objective/header/status stasiun/dock/navigation; `QA/campaign-android-golden-recipes.png`, nama/statistik/biaya dalam baris rapat.

## E2 — Visual focus and affordances

- `WorkshopView.cs:92-124,129-186,198-215,220-231` serta screenshot bengkel: header merek, empat resource, objective, chapter, empat label stasiun, informasi waktu, tombol peta, dock, dan lima tab hadir pada satu permukaan.
- `Game/Assets/ForgeEmpire/Runtime/WorkshopView.Town.cs:23-30`: Nightfall ditambahkan ke scene setelah scene/badge dibangun; world clock dan shortcut peta berada di HUD tersendiri. Pencahayaan dunia dapat ikut menggelapkan label/badge di dalam scene. Kesan visual: latar, pekerja, dan area kerja banyak memakai nilai gelap yang serupa, sehingga pusat aksi kurang menonjol.
- `WorkshopView.cs:97-102,223-229`: header Crew memakai ikon 5, bag memakai ikon 2, Settings ikon 7; tab Heroes memakai ikon 5 dan World ikon 7. Screenshot awal menunjukkan ikon World menyerupai roda gigi yang juga dipakai Settings. Ikon belum membedakan tujuan dengan jelas.
- Palet dasar tetap ada dan konsisten: `ForgeUI.cs:9-16`, navy, cream, gold, teal. Pixel art bengkel, hero, perlengkapan, dan peta sudah memiliki identitas; audit tidak mengusulkan membuang aset tersebut.
- Ada 20 token hex berbeda yang direferensikan dalam helper UI dan lima partial view; tidak menghitung warna tekstur atau tint RGBA. Rasio kontras nominal terendah dari pasangan foreground/background solid aktif yang diperiksa adalah 3,7235:1 untuk Gold pada shortcut hasil ekspedisi hijau (`WorkshopView.Town.cs:30,65`); kontras teks di atas artwork belum terukur. Metode dan pasangan ada dalam laporan visual.

## E3 — Navigation and workflow

- `WorkshopView.Modals.cs:13-38`: setiap pembukaan Dialog memanggil CloseModal; menutup dialog tidak memiliki stack kembali ke layar asal.
- `WorkshopView.Campaign.cs:86-124`: memilih resep menutup modal lalu fokus ke forge; mulai produksi masih tindakan terpisah. Tombol SELECT bisa aktif walau bahan belum cukup; syaratnya unlock dan forge tidak sedang berjalan.
- `WorkshopView.Campaign.cs:129-147`, `WorkshopView.Modals.cs:84-136`: equip hero diarahkan ke inventory umum. Setelah pasang, inventory dibuka ulang; konteks persiapan ekspedisi tidak dilanjutkan otomatis.
- `WorkshopView.Campaign.cs:171-203`: peta → region → rute → battle; peta juga mengarahkan pemain ke persiapan hero. Lokasi dan konteks berpindah melalui modal pengganti.
- `WorkshopView.cs:262-265`: kartu objective di bengkel membuka daftar quest, bukan langsung pekerjaan untuk memenuhi objective.
- `WorkshopView.Campaign.cs:38-61,86-103`: quest membuat 15 baris per kategori; resep membuat semua 20 baris termasuk terkunci. Ini jumlah isi list, bukan jumlah yang terlihat bersamaan.

## E4 — First session and item purpose

- `WorkshopView.Modals.cs:40-47`: welcome menjelaskan `MINE > SMELT > FORGE > SELL`, lalu dalam paragraf lain memperkenalkan strike, quest, equip hero, dan peta. Begin langsung mengubah TutorialStep dari 0 ke 1.
- `Game/Assets/ForgeEmpire/Core/WorkshopSimulation.cs:46-70`: smelt memakan 2 ore; forge memakan bahan resep; market mengambil item inventory pertama saat penjualan dimulai.
- `WorkshopView.Modals.cs:53-55`: bantuan untuk pause merchant agar gear tersimpan ada di halaman Crew. Maka perlindungan barang untuk hero/quest bergantung pada pemain menemukan kontrol pause dan mengerti prioritas FIFO.
- `Game/Assets/ForgeEmpire/Core/GameContent.cs:38-57,78-98`: resep punya fungsi weapon/armor dan kebutuhan material; alur cerita memerlukan craft, ekspedisi, serta boss. Hasil tempa punya lebih dari satu kegunaan: jual, equip, dan penyerahan quest.

## E5 — Existing gameplay depth and presentation

- `WorkshopSimulation.cs:110-126`: timing akurat meningkatkan quality sampai 3; strike membantu progress. `WorkshopView.cs:212-215` menggambar area timing hanya setinggi 22 unit root, atau 7,33 pada normalisasi lebar 360; garis track di dalamnya bahkan hanya 11 unit.
- `GameContent.cs:38-57`: weakness elemen dan armor resistance dijelaskan dalam deskripsi perlengkapan. Perbedaan fungsional sudah ada, tetapi membutuhkan membaca halaman detail.
- `WorkshopView.Battle.cs:37-46,55-69`: tiga skill aktif, cooldown, heavy-attack hint, serta hasil loot sudah disediakan. Ini bukti fitur tersedia, bukan alasan menambah tiga skill baru.
- `WorkshopView.Town.cs:58-67`: restorasi kota dan keadaan ekspedisi sudah terhubung ke bengkel; peningkatan visual dapat memperjelas dampaknya.

## E6 — Honest labels and state accuracy

- `WorkshopApp.cs:48` mengatakan penjualan menumbuhkan reputation; `WorkshopSimulation.cs:261-264` hanya menambah gold dan TotalSold.
- `WorkshopView.Modals.cs:79` mengatakan komisi membangun reputation; `WorkshopSimulation.cs:212-217` hanya memberi gold dan menambah OrdersCompleted.
- `WorkshopView.Modals.cs:55` menjelaskan smith bekerja ketika dua ingot tersedia; biaya aktual resep bervariasi 2–11 ingot plus material langka (`GameContent.cs:38-57`).
- `WorkshopView.Battle.cs:46` menggunakan footer ekspedisi terus berjalan untuk seluruh state; screenshot `QA/campaign-android-golden-skill-patrol-battle-live.png` justru menunjukkan victory dan tombol Collect Loot. Nama berkas tidak selalu mewakili state di dalam gambarnya.
- Tidak ditemukan monetisasi, iklan, timer pembelian, atau pola langganan dalam jalur kode yang diperiksa; ini bukan audit monetisasi seluruh produk.

## E7 — States and access

- Empty inventory: `WorkshopView.Modals.cs:95-108`; success/feedback: `WorkshopApp.cs:47-48`; save error: `WorkshopApp.cs:150-155`; disabled actions: `WorkshopView.cs:365-369`; disabled/cooldown battle: `WorkshopView.Battle.cs:59-69`.
- `ForgeUI.cs:57`: setiap Button menonaktifkan Navigation.Mode; tidak ada traversal fokus yang disusun untuk kontrol utama. Escape menutup modal (`WorkshopView.cs:315`). ARIA dan skip link tidak berlaku untuk aplikasi native Unity.
- Halaman settings hanya menyediakan sound/music/haptics (`WorkshopView.Modals.cs:138-145`); pengaturan ukuran teks dan pengurangan gerak belum disediakan di sana.
- Loading tidak diamati; layar dibangun sinkron. Tidak diberi klaim pengujian TalkBack atau kompatibilitas screen reader karena belum diuji.

## E8 — Weight and attention

Lihat [evidence-weight.md](evidence-weight.md) untuk ukuran build, resource, delapan kanal animasi idle yang dijadwalkan, serta batas data performa historis. Metrik JavaScript/browser tidak berlaku pada runtime native.

## E9 — Screenshot provenance and audit limits

- Primer: layar kampanye dengan lima tab di `QA/campaign-android-*.png`, cocok dengan struktur kode saat audit.
- Pendukung historis: `QA/android-compact-screen.png` masih memakai empat tab lama; **tidak dipakai untuk menilai navigasi terkini**.
- `QA/campaign-android-result.json` mencatat putusnya emulator dan `passed: false`. Audit tidak menyatakan playthrough terbaru lulus.
- Audit ini tidak mengukur pemahaman pemain baru, ukuran fisik di ponsel pengguna, waktu penyelesaian tugas, cold start, atau baterai. Temuan perilaku adalah inferensi dari kode kecuali diikuti screenshot yang sesuai.

## Evidence reports

- [Struktur dan interaksi](evidence-structure.md): jumlah kontrol per layar, kedalaman hierarchy, duplikasi tujuan, langkah navigasi, fokus keyboard, dan batas pengukuran.
- [Visual](evidence-visual.md): skala teks/spacing, 20 token hex, perhitungan kontras, state checklist, ukuran objek, serta wilayah screenshot.
- [Gameplay dan copy](evidence-gameplay-copy.md): ekonomi, progres, pilihan barang, battle, ketidaksesuaian teks/perilaku, dan usulan kata-kata.
- [Bobot dan perhatian](evidence-weight.md): build/resource, gerak idle, modal awal, batas performa.
- [Katalog string](runtime-ui-string-catalog.tsv): 423 entri sumber dengan file/baris. Katalog merupakan kutipan kode untuk pelacakan; bukan seluruh teks yang pasti tampil serentak di runtime.
