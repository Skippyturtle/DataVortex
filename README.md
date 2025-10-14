![image](https://github.com/user-attachments/assets/3916aa66-d1b8-4c66-b3c7-62a1207625a8)


## Il est impératif d'avoir installé NET 7.0, si ce n'est pas déjà fait, voici le lien : https://dotnet.microsoft.com/fr-fr/download/dotnet/7.0


# DataVortex

DataVortex est un logiciel conçu pour automatiser le téléchargement et le traitement de fichiers d'archives depuis Telegram. Il analyse les fichiers extraits à la recherche d'informations, et les sauvegarde dans des fichiers de résultats.

## Fonctionnalités

- **Téléchargement de fichiers :** Récupération automatique des archives depuis les canaux Telegram auxquels vous êtes abonné.
- **Extraction et analyse :** Décompression des archives et extraction des informations pertinentes.
- **Filtrage par mots-clés :** Recherchez les informations en fonction des mots-clés définis.
- **Interface console :** Suivi en temps réel des opérations et gestion des archives.

## Prérequis

Avant de commencer, assurez-vous d'avoir les éléments suivants :

Rendez vous sur [Telegram](https://my.telegram.org/) pour récupérer votre ID API ainsi que votre HASH API.

- **API ID et API Hash de Telegram :** Nécessaires pour accéder à l'API de Telegram.
- **Numéro de téléphone :** Votre numéro de téléphone associé à votre compte Telegram.
- **Webhooks (si applicable) :** Configurez vos webhooks pour recevoir les mises à jour des canaux.

## Configuration

### 1. Fichier de Configuration

Le fichier de configuration `telegram.json` doit être créé dans le dossier `Config`. Il contient les informations suivantes :

- `ApiId` : Votre ID API de Telegram.
- `ApiHash` : Votre hash API de Telegram.
- `PhoneNumber` : Votre numéro de téléphone associé à Telegram.
- `VerificationCode` : Code de vérification envoyé par SMS (sera demandé lors de la première exécution).
- `Password` : Mot de passe pour la vérification en deux étapes si activé.

**Exemple de `telegram.json`**

```json
{
  "ApiId": "123456",
  "ApiHash": "abcdef1234567890abcdef1234567890",
  "PhoneNumber": "+33123456789",
  "VerificationCode": "12345",
  "Password": "your_2fa_password"
}
```

### 2. Mots-Clés

Les mots-clés sont utilisés pour filtrer les informations extraites des fichiers. Ils sont définis dans le fichier Keywords.json situé dans le dossier `Config`. Ce fichier est une liste de mots-clés, et vous pouvez ajouter ou modifier les mots-clés en fonction de vos besoins.

Exemple de `Keywords.json`

```
[
  "google",
  "facebook"
]
```
### 3. Webhooks

Si votre système nécessite des webhooks pour recevoir les mises à jour, assurez-vous de les configurer correctement. Consultez la documentation de Telegram pour savoir comment configurer les webhooks.

Utilisation
**Exécutez le logiciel :** Lancez le programme pour commencer le téléchargement et l'analyse des archives.
**Interaction avec la Console :** Le programme affichera des messages et des instructions dans la console. Suivez les instructions pour arrêter le programme ou effectuer des actions nécessaires.

## Notes

Assurez-vous que les permissions de votre répertoire de téléchargement et de traitement sont correctement configurées.
Le logiciel crée des répertoires temporaires `dbdtemp` et de résultats `Result`. Assurez-vous d'avoir suffisamment d'espace disque.
Pour éviter tout problème, assurez-vous que votre compte Telegram est correctement configuré et que vous avez accès aux canaux nécessaires.

### Support

Pour toute question ou problème, veuillez me contacter pour un support technique sur discord `DendiMirrorV2`.


N'hésitez pas à me faire savoir si vous avez besoin d'autres modifications ou ajouts !
