import React, { useEffect, useState, useRef } from "react";
import {useHistory, useParams} from "react-router-dom";
import * as api from '../api/AxiosAdminRoom';
import { getLanguages } from '../api/AxiosTranslation';
import { getVisitorTypes } from '../api/AxiosVisitorType';
import Panorama360 from './Panorama360';
import { Buffer } from 'buffer';
import { toast } from "sonner";
import Loader from "./Loader";
import Masonry from 'react-masonry-css';
import '../style/AdminRoomDetails.css';
import {FaArrowLeft, FaPen, FaTrash, FaPlusCircle, FaLanguage, FaGlobe} from "react-icons/fa";
import {ImLocation2} from "react-icons/im";
import {MdOutlineFileUpload} from "react-icons/md";
import ModalAddEditImage from "./room_details/ModalAddEditImage";
import ConfirmDialog from "./dialogs/ConfirmDialog";
import { useTranslation } from 'react-i18next';

const AdminRoomDetails = () => {
  const { t } = useTranslation('adminRoomDetails');
  const { id } = useParams();
  const [pictures, setPictures] = useState([]);
  const [selectedPicture, setSelectedPicture] = useState('');
  const [selectedPictureId, setSelectedPictureId] = useState('');
  const [infoPopups, setInfoPopups] = useState([]);
  const [links, setLinks] = useState([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [showAllInfospots, setShowAllInfospots] = useState(true);
  const [allInfoPopups, setAllInfoPopups] = useState([]);
  const [searchLinkTerm, setSearchLinkTerm] = useState('');
  const [showAllLinks, setShowAllLinks] = useState(true);
  const [allLinks, setAllLinks] = useState([]);
  const [newInfospotModalOpen, setNewInfospotModalOpen] = useState(false);
  const [newLinkModalOpen, setNewLinkModalOpen] = useState(false);
  const [posX, setPosX] = useState('');
  const [posY, setPosY] = useState('');
  const [posZ, setPosZ] = useState('');
  const [isSelectingPosition, setIsSelectingPosition] = useState(false);
  const [selectedImageId, setSelectedImageId] = useState('');
  const [roomName, setRoomName] = useState('');
  const dataFetchedRef = useRef(false);
  const firstLoad = useRef(true);
  const [linkToEdit, setLinkToEdit] = useState(null);
  const [editLinkMod, setEditLinkMod] = useState(false);
  const [infospotToEdit, setInfospotToEdit] = useState(null);
  const [editInfospotMod, setEditInfospotMod] = useState(false);
  const [addImageModalOpen, setAddImageModalOpen] = useState(false);
  const [newImage, setNewImage] = useState(null);
  const [modalSelectedPicture, setModalSelectedPicture] = useState('');
  const [modalInfoPopups, setModalInfoPopups] = useState([]);
  const [modalLinks, setModalLinks] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingModal, setIsLoadingModal] = useState(true);
  const [disableBackgroundClick, setDisableBackgroundClick] = useState(false);
  const [imageToUpdate, setImageToUpdate] = useState(null);

  const [loading, setLoading] = useState(true);
  const [textLoading, setTextLoading] = useState("");

  const [showConfirm, setShowConfirm] = useState(false);
  const [confirmTitle, setConfirmTitle] = useState("");
  const [confirmMessage, setConfirmMessage] = useState("");
  const [imageToDelete, setImageToDelete] = useState(null);
  const [infospotToDelete, setInfospotToDelete] = useState(null);
  const [linkToDelete, setLinkToDelete] = useState(null);

  // Languages and visitor types for InfoPopup form
  const [languages, setLanguages] = useState([]);
  const [visitorTypes, setVisitorTypes] = useState([]);
  const [selectedLanguage, setSelectedLanguage] = useState('');
  const [selectedVisitorType, setSelectedVisitorType] = useState('');

  // Translation modal state
  const [translationModalOpen, setTranslationModalOpen] = useState(false);
  const [infospotForTranslation, setInfospotForTranslation] = useState(null);
  const [infospotTranslations, setInfospotTranslations] = useState([]);
  const [newTranslationTitle, setNewTranslationTitle] = useState('');
  const [newTranslationText, setNewTranslationText] = useState('');

  const history = useHistory();

  // Group InfoPopups by their ID to show translations together
  const groupInfoPopupsById = (popups) => {
    const grouped = {};
    popups.forEach(popup => {
      if (!grouped[popup.id_info_popup]) {
        grouped[popup.id_info_popup] = {
          id_info_popup: popup.id_info_popup,
          id_pictures: popup.id_pictures,
          position_x: popup.position_x,
          position_y: popup.position_y,
          position_z: popup.position_z,
          image_path: popup.image_path,
          translations: []
        };
      }
      grouped[popup.id_info_popup].translations.push({
        id_languages: popup.id_languages,
        title: popup.title,
        text: popup.text,
        id_visitor_type: popup.id_visitor_type
      });
    });
    return Object.values(grouped);
  };

  const showLoading = (promises, textLoading, textSuccess, textError) => {
    setLoading(true);
    setTextLoading(textLoading);
    // Return a toaster success after all promises are resolved
    Promise.all(promises)
        .then(() => {
          setLoading(false);
          toast.success(textSuccess);
        })
        .catch((error) => {
          setLoading(false);
          console.error('Error fetching data:', error);
          toast.error(textError);
        });
  }

  const fetchAllData = async () => {
    try {
      const roomData = await api.getRoomDetails(id);
      setRoomName(roomData.name);

      const picturesData = await api.getPicturesByRoomId(id);
      const picturesWithUrls = await Promise.all(
        picturesData.map(async (pic) => ({
          ...pic,
          imageUrl: await api.getImage(pic.id_pictures),
        }))
      );
      setPictures(picturesWithUrls);
      if (picturesWithUrls.length > 0) {
        const firstPicture = picturesWithUrls[0];
        setSelectedPicture(firstPicture);
        handlePictureClick(firstPicture, firstPicture.id_pictures);
      }
      const allInfoPopups = await Promise.all(
        picturesData.map(async (pic) => await api.getInfoPopup(pic.id_pictures))
      );
      setAllInfoPopups(allInfoPopups.flat());

      const allLinks = await Promise.all(
        picturesData.map(async (pic) => await api.getLinks(pic.id_pictures))
      );
      setAllLinks(allLinks.flat());

      // Fetch languages and visitor types for InfoPopup form
      try {
        const languagesData = await getLanguages();
        setLanguages(languagesData || []);
        if (languagesData && languagesData.length > 0) {
          setSelectedLanguage(languagesData[0].id_language);
        }
      } catch (err) {
        console.error('Error fetching languages:', err);
      }

      try {
        const visitorTypesData = await getVisitorTypes();
        setVisitorTypes(visitorTypesData || []);
      } catch (err) {
        console.error('Error fetching visitor types:', err);
      }
    } catch (error) {
      console.error('Error fetching data:', error);
    }
  };

  useEffect(() => {
    if (!dataFetchedRef.current) {
      const fetchAllDataPromise = fetchAllData();
      showLoading([fetchAllDataPromise], t('loadingRoomDetails'), t('roomDetailsLoaded'), t('errorLoadingDetails'));
      fetchAllDataPromise.then(() => {
        setIsLoading(false);
        firstLoad.current = false;
      });
      dataFetchedRef.current = true;
    }
  }, [id]);

  const handlePictureClick = async (picture, pictureId) => {
    setIsLoading(true);
    const getInfoPopupPromise = getInfoPopup(pictureId);
    const getLinksPromise = getLinks(pictureId);
    if(!firstLoad.current) {
      showLoading([getInfoPopupPromise, getLinksPromise], t('loadingRoomDetails'), t('roomDetailsLoaded'), t('errorLoadingDetails'));
    }
    Promise.all([getInfoPopupPromise,getLinksPromise]).then(() => {
    setSelectedPicture(picture);
    setSelectedPictureId(pictureId);
    setIsLoading(false);
    });
  };

  const getInfoPopup = async (id_pictures) => {
    const infoPopups = await api.getInfoPopup(id_pictures);
    setInfoPopups(infoPopups);
    return infoPopups;
  };

  const getLinks = async (id_pictures) => {
    const links = await api.getLinks(id_pictures);
    setLinks(links);
    return links;
  }

  const handleLinkClick = async (pictureId) => {
    const picture = pictures.find(pic => pic.id_pictures === pictureId);
    handlePictureClick(picture, pictureId);
  };

  const filteredInfoPopups = infoPopups.filter(popup =>
    (popup.title || '').toLowerCase().includes(searchTerm.toLowerCase())
  );

  const filteredLinks = links.filter(link =>
    link.id_pictures_destination.toString().includes(searchLinkTerm)
  );

  const displayedInfoPopups = showAllInfospots
    ? allInfoPopups.filter(popup => (popup.title || '').toLowerCase().includes(searchTerm.toLowerCase()))
    : filteredInfoPopups;

  const displayedLinks = showAllLinks 
    ? allLinks.filter(link => link.id_pictures_destination.toString().includes(searchLinkTerm))
    : filteredLinks;

  const handleNewInfospotSubmit = async (event) => {
    event.preventDefault();
    const formData = new FormData(event.target);
    if (formData.get('pic').size === 0) {
        alert(t('pleaseSelectInfospotImage'));
        return;
    }

    // Add language and visitor type to form data
    if (selectedLanguage) {
      formData.append('id_languages', selectedLanguage);
    }
    if (selectedVisitorType) {
      formData.append('id_visitor_type', selectedVisitorType);
    }

    const insertPromise = api.insertInfoPopUp(formData);

    const updatedInfoPopupsPromise = insertPromise.then(async () => {
      await getInfoPopup(selectedPictureId);

      const allImageInfoPopups = await Promise.all(
          pictures.map(async (pic) => await api.getInfoPopup(pic.id_pictures))
      );
      setAllInfoPopups(allImageInfoPopups.flat());
    });

    showLoading([insertPromise, updatedInfoPopupsPromise], t('addingInfospot'), t('infospotAddedSuccess'), t('errorAddingInfospot'));

    setNewInfospotModalOpen(false);
    setDisableBackgroundClick(false);
    cancelSelectPosition();
  };

  const handleNewLinkSubmit = async (event) => {
    event.preventDefault();
    const formData = new FormData(event.target);
    const insertPromise = api.insertLink(formData);

    const updateLinksPromise = insertPromise.then(async () => {
        await getLinks(selectedPictureId);
        await getInfoPopup(selectedPictureId);
        const allImageLinks = await Promise.all(
            pictures.map(async (pic) => await api.getLinks(pic.id_pictures))
        );
        setAllLinks(allImageLinks.flat());
    });

    showLoading([insertPromise, updateLinksPromise], t('addingLink'), t('linkAddedSuccess'), t('errorAddingLink'));

    setNewLinkModalOpen(false);
    setDisableBackgroundClick(false);
    cancelSelectPosition();
  };

  const handlePositionSelect = (position) => {
    setPosX(position.x);
    setPosY(position.y);
    setPosZ(position.z);
    setIsSelectingPosition(false);
  };

  const handleModalInfopopup = () => {
    setNewInfospotModalOpen(true);
    setDisableBackgroundClick(true);
    setPosX(null);
    setPosY(null);
    setPosZ(null);
    // Reset language and visitor type selections
    if (languages.length > 0) {
      setSelectedLanguage(languages[0].id_language);
    }
    setSelectedVisitorType('');

    if (pictures.length > 0) {
      const firstPicture = pictures[0];
      setIsLoadingModal(true);

      setModalSelectedPicture(selectedPicture);
      setSelectedImageId(selectedPictureId);

      const infospotsPromise = api.getInfoPopup(selectedPictureId)
      const linksPromise = api.getLinks(selectedPictureId);

      infospotsPromise.then((infospots) => {
        setModalInfoPopups(infospots);
      });

      linksPromise.then((links) => {
        setModalLinks(links);
      });
      showLoading([infospotsPromise, linksPromise], t('loadingRoomDetails'), t('roomDetailsLoaded'), t('errorLoadingDetails'));
      Promise.all([infospotsPromise, linksPromise]).then(() => {
        setIsLoadingModal(false);
      });
    }
  }

  const closeModalInfospot = () => {
    setNewInfospotModalOpen(false);
    setDisableBackgroundClick(false);
    setEditInfospotMod(false);
    setInfospotToEdit(null);
    cancelSelectPosition();
    setPosX('');
    setPosY('');
    setPosZ('');
  }

  const handleModalLink = () => {
    setNewLinkModalOpen(true)
    setDisableBackgroundClick(true);
    setPosX(null);
    setPosY(null);
    setPosZ(null);

    if (pictures.length > 0) {
            setIsLoadingModal(true);
      setModalSelectedPicture(selectedPicture);
      setSelectedImageId(selectedPictureId);

      const infospotsPromise = api.getInfoPopup(selectedPictureId)
      const linksPromise = api.getLinks(selectedPictureId);

      infospotsPromise.then((infospots) => {
        setModalInfoPopups(infospots);
      });

      linksPromise.then((links) => {
        setModalLinks(links);
      });
        showLoading([infospotsPromise, linksPromise], t('loadingRoomDetails'), t('roomDetailsLoaded'), t('errorLoadingDetails'));
      Promise.all([infospotsPromise, linksPromise]).then(() => {
        setIsLoadingModal(false);
      });
    }
  }

  const closeModalLink = () => {
    setNewLinkModalOpen(false);
    setDisableBackgroundClick(false);
    setEditLinkMod(false);
    setLinkToEdit(null);
    cancelSelectPosition();
    setPosX('');
    setPosY('');
    setPosZ('');
  }

  const handleSelectPositionClick = (e) => {
    setIsSelectingPosition(true);
    window.isSelectingPosition = true;
    window.isFirstClick = true;
  };

  const cancelSelectPosition = () => {
    setIsSelectingPosition(false);
    window.isSelectingPosition = false;
    window.isFirstClick = false;
  }

  const handleModalPictureClick = async (imageUrl, pictureId) => {
    setIsLoadingModal(true);
    setModalSelectedPicture(imageUrl);
    setSelectedImageId(pictureId);
    const infospotsPromise = api.getInfoPopup(pictureId);
    const linksPromise = api.getLinks(pictureId);
    showLoading([infospotsPromise, linksPromise], 'Chargement des détails de la pièce...', 'Chargement des détails réussi', 'Erreur lors du chargement des détails');
    infospotsPromise.then((infospots) => {
        setModalInfoPopups(infospots);
    });
    linksPromise.then((links) => {
        setModalLinks(links);
    });
    Promise.all([infospotsPromise, linksPromise]).then(() => {
      setIsLoadingModal(false);
    });
  };

  const handleDeleteInfoPopup = (event, id) => {
    event.stopPropagation();
    event.preventDefault();
    setInfospotToDelete(id);
    setConfirmTitle(t('deleteInfospotTitle'));
    setConfirmMessage(t('deleteInfospotConfirm'));
    setShowConfirm(true);
  }

  const confirmDeleteInfospot = async () => {
    try {
      const deletePromise = api.deleteInfospot(infospotToDelete);
      deletePromise.then(() => {
        const updatedInfoPopups = infoPopups.filter(popup => popup.id_info_popup !== infospotToDelete);
        setInfoPopups(updatedInfoPopups);
        const updatedAllInfoPopups = allInfoPopups.filter(popup => popup.id_info_popup !== infospotToDelete);
        setAllInfoPopups(updatedAllInfoPopups);
        setInfospotToDelete(null);
        toast.success(t('infospotDeleted'));
      });
    } catch (error) {
      console.error('Error deleting infopopup:', error);
    }
  }

  const handleEditInfoPopup = async (event, popup) => {
    setDisableBackgroundClick(true);
    setInfospotToEdit(popup);
    setEditInfospotMod(true);
    setPosX(popup.position_x);
    setPosY(popup.position_y);
    setPosZ(popup.position_z);
    // Pre-fill language and visitor type from popup data
    setSelectedLanguage(popup.id_languages || (languages.length > 0 ? languages[0].id_language : ''));
    setSelectedVisitorType(popup.id_visitor_type || '');
    const image = pictures.find(pic => pic.id_pictures === popup.id_pictures);
    handleModalPictureClick(image, popup.id_pictures);
    setNewInfospotModalOpen(true);
  }

  const handleEditInfospotSubmit = async (event) => {
    event.preventDefault();
    const formData = new FormData(event.target);
    if (formData.get('pic').size === 0) {
        formData.delete('pic');
    }
    formData.append('id_info_popup', infospotToEdit.id_info_popup);

    // Add language and visitor type to form data
    if (selectedLanguage) {
      formData.append('id_languages', selectedLanguage);
    }
    if (selectedVisitorType) {
      formData.append('id_visitor_type', selectedVisitorType);
    }

    const updatePromise = api.updateInfospot(formData);

    const updatedInfoPopupsPromise = updatePromise.then(async () => {
      await getInfoPopup(selectedPictureId);
      const allImageInfoPopups = await Promise.all(
          pictures.map(async (pic) => await api.getInfoPopup(pic.id_pictures))
      );
      setAllInfoPopups(allImageInfoPopups.flat());
    });

    showLoading([updatePromise, updatedInfoPopupsPromise], t('updatingInfospot'), t('infospotUpdatedSuccess'), t('errorUpdatingInfospot'));

    setNewInfospotModalOpen(false);
    setDisableBackgroundClick(false);
    setEditInfospotMod(false);
  }

  const handleDeleteLink = (event, id) => {
    event.stopPropagation();
    event.preventDefault();
    setLinkToDelete(id);
    setConfirmTitle(t('deleteLinkTitle'));
    setConfirmMessage(t('deleteLinkConfirm'));
    setShowConfirm(true);
  }

  const confirmDeleteLink = async () => {
    try {
      const deletePromise = api.deleteLink(linkToDelete);
      deletePromise.then(() => {
        const updatedLinks = links.filter(link => link.id_links !== linkToDelete);
        setLinks(updatedLinks);
        const updatedAllLinks = allLinks.filter(link => link.id_links !== linkToDelete);
        setAllLinks(updatedAllLinks);
        setLinkToDelete(null);
        toast.success(t('linkDeleted'));
      });
    } catch (error) {
      console.error('Error deleting link:', error);
    }
  }

  const handleEditLink = async (event, link) => {
    setDisableBackgroundClick(true);
    setLinkToEdit(link);
    setEditLinkMod(true);
    setPosX(link.position_x);
    setPosY(link.position_y);
    setPosZ(link.position_z);
    const imageUrl = pictures.find(pic => pic.id_pictures === link.id_pictures).imageUrl;
    handleModalPictureClick(imageUrl, link.id_pictures);
    setNewLinkModalOpen(true);
  }

  const handleEditLinkSubmit = async (event) => {
    event.preventDefault();
    const formData = new FormData(event.target);
    formData.append('id_links', linkToEdit.id_links);
    const insertPromise = api.updateLink(formData);

    const updatedLinksPromise = insertPromise.then(async () => {
        await getLinks(selectedPictureId);
        await getInfoPopup(selectedPictureId);
        const allImageLinks = await Promise.all(
            pictures.map(async (pic) => await api.getLinks(pic.id_pictures))
        );
        setAllLinks(allImageLinks.flat());
    });

    showLoading([insertPromise, updatedLinksPromise], t('updatingLink'), t('linkUpdatedSuccess'), t('errorUpdatingLink'));

    setNewLinkModalOpen(false);
    setDisableBackgroundClick(false);
    setEditLinkMod(false);
  }

  const handleEditPicture = async (id) => {
    setImageToUpdate(pictures.find(pic => pic.id_pictures === id));
    setAddImageModalOpen(true);
  }

  const handleDeletePicture = async (id) => {
    setImageToDelete(id);
    setConfirmTitle(t('deleteImageTitle'));
    setConfirmMessage(t('deleteImageConfirm'));
    setShowConfirm(true);
  }

  const confirmDeleteImage = async () => {
    const deletePromise = api.deleteImage(imageToDelete);
    const updatedPicturesPromise = deletePromise.then(async () => {
      setImageToDelete(null);
      await fetchAllData();
    });
    showLoading([deletePromise, updatedPicturesPromise], t('deletingImage'), t('imageDeletedSuccess'), t('errorDeletingImage'));
  }

  const reloadAfterAddEditImage = async (type) => {
    if (type === 'add') {
      const reloadPromise = fetchAllData();
      setAddImageModalOpen(false);
      showLoading([reloadPromise], t('addingImage'), t('imageAddedSuccess'), t('errorAddingImage'));
    } else if (type === 'edit') {
      const reloadPromise =  fetchAllData();
      setImageToUpdate(null);
      setAddImageModalOpen(false);
      showLoading([reloadPromise], t('updatingImage'), t('imageUpdatedSuccess'), t('errorUpdatingImage'));
    }
  };

  const breakpointColumnsObj = {
    default: 2,
    1075: 1,  // Passe à une seule colonne pour les écrans <= 1075px
    700: 1,
  };

  // Open translation modal for an existing InfoPopup
  const handleOpenTranslationModal = async (infoPopup) => {
    setInfospotForTranslation(infoPopup);
    setTranslationModalOpen(true);
    setNewTranslationTitle('');
    setNewTranslationText('');
    if (languages.length > 0) {
      setSelectedLanguage(languages[0].id_language);
    }
    setSelectedVisitorType('');

    // Load existing translations
    try {
      const translations = await api.getInfospotTranslations(infoPopup.id_info_popup);
      setInfospotTranslations(translations || []);
    } catch (err) {
      console.error('Error loading translations:', err);
      setInfospotTranslations([]);
    }
  };

  // Close translation modal
  const closeTranslationModal = () => {
    setTranslationModalOpen(false);
    setInfospotForTranslation(null);
    setInfospotTranslations([]);
    setNewTranslationTitle('');
    setNewTranslationText('');
  };

  // Add a new translation to an existing InfoPopup
  const handleAddTranslation = async (e) => {
    e.preventDefault();
    if (!infospotForTranslation || !selectedLanguage || !newTranslationTitle || !newTranslationText) {
      toast.error(t('fillAllFields'));
      return;
    }

    // Check if translation for this language already exists
    const existingTranslation = infospotTranslations.find(t => t.id_languages === parseInt(selectedLanguage));
    if (existingTranslation) {
      toast.error(t('translationAlreadyExists'));
      return;
    }

    try {
      await api.addInfospotTranslation(
        infospotForTranslation.id_info_popup,
        newTranslationTitle,
        newTranslationText,
        parseInt(selectedLanguage),
        selectedVisitorType ? parseInt(selectedVisitorType) : null
      );
      toast.success(t('translationAddedSuccess'));

      // Reload translations
      const translations = await api.getInfospotTranslations(infospotForTranslation.id_info_popup);
      setInfospotTranslations(translations || []);

      // Reload all infopopups
      const allImageInfoPopups = await Promise.all(
        pictures.map(async (pic) => await api.getInfoPopup(pic.id_pictures))
      );
      setAllInfoPopups(allImageInfoPopups.flat());
      await getInfoPopup(selectedPictureId);

      // Reset form
      setNewTranslationTitle('');
      setNewTranslationText('');
    } catch (err) {
      console.error('Error adding translation:', err);
      toast.error(t('errorAddingTranslation'));
    }
  };

  // Delete a translation
  const handleDeleteTranslation = async (id_languages) => {
    if (!infospotForTranslation) return;

    if (!window.confirm(t('confirmDeleteTranslation'))) return;

    try {
      await api.deleteInfospotTranslation(infospotForTranslation.id_info_popup, id_languages);
      toast.success(t('translationDeleted'));

      // Reload translations
      const translations = await api.getInfospotTranslations(infospotForTranslation.id_info_popup);
      setInfospotTranslations(translations || []);

      // Reload all infopopups
      const allImageInfoPopups = await Promise.all(
        pictures.map(async (pic) => await api.getInfoPopup(pic.id_pictures))
      );
      setAllInfoPopups(allImageInfoPopups.flat());
      await getInfoPopup(selectedPictureId);
    } catch (err) {
      console.error('Error deleting translation:', err);
      toast.error(t('errorDeletingTranslation'));
    }
  };

  // Get language name by ID
  const getLanguageName = (id_languages) => {
    const lang = languages.find(l => l.id_language === id_languages);
    return lang ? lang.name_language : `${t('languageLabel')} ${id_languages}`;
  };

  // Get visitor type name by ID
  const getVisitorTypeName = (id_visitor_type) => {
    if (!id_visitor_type) return t('allVisitorsShort');
    const vt = visitorTypes.find(v => v.id_visitor_type === id_visitor_type);
    return vt ? vt.name_visitor_type : `${t('typeFallback')} ${id_visitor_type}`;
  };

  // Group displayed infopopups
  const groupedDisplayedInfoPopups = groupInfoPopupsById(displayedInfoPopups);

  return (
    <div className="admin-room-details-page">
      <Loader show={loading} text={textLoading} />

      <div className={`fixed z-40 ${newInfospotModalOpen || newLinkModalOpen || addImageModalOpen ? 'pointer-events-none opacity-50' : ''}`} style={{left: "20px", top: "80px"}}>
        <button
            onClick={() => history.push('/admin/room')}
            className="px-4 py-2 button-type font-title font-bold flex items-center gap-2">
          <FaArrowLeft /> {t('back')}
        </button>
      </div>


      

      {/* à mettre dans la navbar*/}
      {/*<div className="text-2xl text-junia-purple font-title font-bold mt-4">{roomName}</div>*/}


      


    <div className="admin-room-details-container flex flex-col items-center p-3">
      
      <div className="image-panorama-container bg-white w-80 rounded-2xl mt-4 flex">

        <div className="image-list flex flex-col p-2 justify-between">
            <div className="button-add-360 flex justify-center ">
                        <button onClick={() => {
                          setAddImageModalOpen(true)
                          setImageToUpdate(null);
                        }} className="button-type font-title font-bold text-2xl p-2">
                          {t('addImage360')}
                        </button>
            </div>
          {pictures.map(picture => (
              <div key={picture.id_pictures} className="w-40vw p-1">
                <div className="relative">
                  <img
                      src={picture.imageUrl}
                      alt={`Aperçu de ${picture.id_pictures}`}
                      onClick={() => handlePictureClick(picture, picture.id_pictures)}
                      className="image-card rounded-lg cursor-pointer shadow hover:shadow-lg transition-shadow duration-300 w-full"
                  />
                  <div className="flex gap-1 absolute" style={{ bottom: '5px', right: '5px' }}>
                    <button
                        onClick={() => handleEditPicture(picture.id_pictures)}
                        className="px-2 py-2 button-type"
                    >
                      <FaPen />
                    </button>
                    <button
                        onClick={() => handleDeletePicture(picture.id_pictures)}
                        className="px-2 py-2 button-type2"
                    >
                      <FaTrash />
                    </button>
                  </div>
                </div>
              </div>
          ))}
        </div>

        <div className={`flex-3 relative w-70  p-4 ${newInfospotModalOpen || newLinkModalOpen ? 'pointer-events-none opacity-50' : ''}`} style={{flex: '3'}}>
          {selectedPicture && (
            <Panorama360
              infoPopups={infoPopups}
              selectedPicture={selectedPicture}
              links={links}
              onLinkClick={handleLinkClick}
              onPositionSelect={null}
              isLoading={isLoading}
              disableClick={disableBackgroundClick}
              className=""
            />
          )}
        </div>

      </div>

      {/* Nouveau conteneur flex pour affichage côte à côte */}
      <div className="content-container">
        {/* Section des infospots (2/3 de la largeur) */}
        <div className="infospots-section" style={{width: '66%'}}>
          {/* Zone de recherche d'infospots en haut des 2/3 gauche */}
          <div className="info-spot-research-zone w-full mb-4">
            <div className="flex gap-4 items-center w-full mb-4">
              <div className="text-white text-4xl bg-junia-purple px-4 py-1 font-title font-bold rounded-full">{t('infospots')}</div>
              <div className="button-type font-bold font-title text-xl px-4 py-2">
                <button onClick={handleModalInfopopup} className="flex items-center gap-2"><FaPlusCircle /> {t('newInfospot')}</button>
              </div>
              
            </div>
            <div className="flex gap-4 justify-between items-center mb-3 w-full">
              <div className="flex gap-4">
                  <input
                    type="text"
                    placeholder={t('searchByTitle')}
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="w-full p-2 bg-white research-input-IS research-input-orange-text rounded-full" />
                
                  <button 
                    onClick={() => setShowAllInfospots(!showAllInfospots)} 
                    className="button-type all-IS-button font-title font-bold px-4 py-2 ">
                    {showAllInfospots ? t('allInfospots') : t('currentImageInfospots')}
                  </button>
              </div>

              
            </div>
          </div>

          {/* Grille d'infospots sous la zone de recherche */}
          <Masonry
            breakpointCols={breakpointColumnsObj}
            className="my-masonry-grid"
            columnClassName="my-masonry-grid_column"
          >
            {groupedDisplayedInfoPopups.map((popup) => (
              <div key={popup.id_info_popup} className="one-info-spot flex flex-col gap-2">
                <div className="font-bold font-title text-2xl"> 
                  <span className="text-junia-purple"> {t('title')} : </span>
                  <span className="text-junia-orange">{popup.title}</span>
                </div>
                <div className="font-bold font-title text-2xl text-junia-purple">{t('description')} :</div>
                <div className="font-texts text-md text-junia-orange text-justify">{popup.text}</div>
                <div className="font-bold font-title text-2xl text-junia-purple">
                  <span className="text-junia-purple"> {t('panoramaId')} : </span>
                  <span className="text-junia-orange">{popup.id_pictures}</span>
                </div>
                <div className="flex justify-center ">
                  {popup.image_path && (
                    <div className="max-h-30">
                      <img src={`/${popup.image_path}`} alt={`Aperçu de ${popup.title}`}/>
                    </div>
                  )}
                </div>
                {/* <div className="flex w-full justify-between ">
                  <button onClick={(event) => handleEditInfoPopup(event, popup)} className="button-type p-2 font-title font-bold flex items-center gap-2"><FaPen /> {t('modify')}</button>
                  <button onClick={(event) => handleDeleteInfoPopup(event, popup.id_info_popup)} className="button-type2 p-2 font-title font-bold flex items-center gap-2"><FaTrash /> {t('delete')}</button>
                </div>
              </div> */}
               <div className="flex w-full gap-2 flex-wrap">
                  <button
                    onClick={() => handleOpenTranslationModal(popup)}
                    className="button-type p-2 font-title font-bold flex items-center gap-2 flex-1"
                  >
                    <FaLanguage /> Traductions
                  </button>
                  <button
                    onClick={(event) => handleEditInfoPopup(event, {...popup, ...popup.translations[0]})}
                    className="button-type p-2 font-title font-bold flex items-center gap-2"
                  >
                    <FaPen />
                  </button>
                  <button
                    onClick={(event) => handleDeleteInfoPopup(event, popup.id_info_popup)}
                    className="button-type2 p-2 font-title font-bold flex items-center gap-2"
                  >
                    <FaTrash />
                  </button>
                </div>
              </div>
            ))}
          </Masonry>
        </div>

        {/* Section des liens (1/3 de la largeur) */}
        <div className=" flex flex-col gap-2 w-1/3">
          <div className="links-research-zone w-full mb-4">
            <div className="flex gap-4 items-center w-full mb-4">
              <div className="text-white text-4xl bg-junia-purple px-4 py-1 font-title font-bold rounded-full">{t('links')}</div>
              <div className="button-type font-bold font-title text-xl px-4 py-2">
                <button onClick={handleModalLink} className="flex items-center gap-2"><FaPlusCircle /> {t('newLink')}</button>
              </div>
            </div>

            <div className="flex gap-4 items-center mb-2 w-full ">
              <div className="flex gap-4">
                <input
                  type="text"
                  placeholder={t('searchByDestinationId')}
                  value={searchLinkTerm}
                  onChange={(e) => setSearchLinkTerm(e.target.value)}
                  className="w-full p-2 bg-white research-input-L research-input-orange-text rounded-full"/>
                
                <button 
                  onClick={() => setShowAllLinks(!showAllLinks)}
                  className="button-type all-IS-button font-title font-bold px-4 py-2">
                  {showAllLinks ? t('allLinks') : t('currentImageLinks')}
                </button>
              </div>

              
            </div>
          </div>

          {displayedLinks.map((link) => (
            <div key={link.id_links} className="one-link-container flex flex-col justify-between items-center bg-white p-2 mb-2.5">
              
              <div className="flex justify-around w-full">
                <div className="flex">
                  <div className="font-bold font-title text-2xl text-junia-purple">ID : </div>
                  <div className="font-bold font-title text-2xl text-junia-orange pl-2"> {link.id_links}</div>
                </div>
                <div className="flex">
                  <div className="font-title text-2xl text-junia-purple">{t('destinationId')} : </div>
                  <div className="font-title text-2xl text-junia-orange pl-2">{link.id_pictures_destination}</div>
                </div>
              </div>
              <div className="flex-1 flex justify-center p-2">
                <img src={pictures.find(pic => pic.id_pictures === link.id_pictures_destination)?.imageUrl} alt={`Destination ${link.id_pictures_destination}`} className="max-w-[100px] max-h-[100px]" />
              </div>
              <div className="flex w-full justify-between px-2">
                <button onClick={(event) => handleEditLink(event, link)} className="button-type p-2 font-title font-bold flex items-center gap-2"><FaPen /> {t('modify')}</button>
                <button onClick={(event) => handleDeleteLink(event, link.id_links)} className="button-type2 p-2 font-title font-bold flex items-center gap-2"><FaTrash /> {t('delete')}</button>
              </div>
            </div>
          ))}
        </div>
      </div>
      </div>

      {/* modales... */}
      {newInfospotModalOpen && (
        <div className="fixed inset-0 flex justify-center items-center modal-background z-50 p-4">
          <div className="modal-infospot">
            {/* En-tête fixe */}
            <div className="modal-header">
              <div className="text-2xl font-bold text-junia-purple font-title text-center flex-grow">
                {editInfospotMod ? t('editInfospot') : t('addNewInfospot')}
              </div>
              <button 
                className="modal-close-button" 
                onClick={closeModalInfospot}
              >
                &times;
              </button>
            </div>

            {/* Section Panorama avec hauteur fixe */}
            <div className="modal-panorama">
              <Panorama360
                infoPopups={modalInfoPopups}
                selectedPicture={modalSelectedPicture}
                links={modalLinks}
                onLinkClick={() => {}}
                onPositionSelect={handlePositionSelect}
                isLoading={isLoadingModal}
              />
            </div>

            {/* Conteneur de formulaire scrollable */}
            <div className="modal-form-container">
              <form onSubmit={editInfospotMod ? handleEditInfospotSubmit : handleNewInfospotSubmit} 
                    className="grid grid-cols-2 gap-4">
                <input type="hidden" name="id_pictures" value={selectedImageId || ''} />
                
                {/* Left column */}
                <div className="flex flex-col gap-4 justify-between h-full">
                  <div className="file-input-container">
                    <div className="file-input-junia">
                      <div className="flex items-center bg-white">
                        <MdOutlineFileUpload className="text-junia-orange text-xl m-2" />
                        <input
                          type="file"
                          accept="image/*"
                          name="pic"
                          className="file:mr-4 file:py-2 file:px-4 file:text-junia-orange file:opacity-70 file:border-0 file:font-title"
                          onChange={(e) => {
                          const file = e.target.files[0];
                          if (!file.type.startsWith("image/")) {
                            alert(t('selectValidImage'));
                            e.target.value = "";
                            }
                          }
                        }
                        />
                      </div>
                    </div>
                  </div>
                  
                  <div className="position-inputs-container flex-col">
                    {/* Labels en haut */}
                    <div className="flex w-full justify-between mb-1">
                      <label className="text-junia-purple font-bold text-center w-1/3">{t('coordinateX')} :</label>
                      <label className="text-junia-purple font-bold text-center w-1/3">{t('coordinateY')} :</label>
                      <label className="text-junia-purple font-bold text-center w-1/3">{t('coordinateZ')} :</label>
                    </div>
                    {/* Inputs en bas */}
                    <div className="flex w-full justify-between">
                      <input 
                        type="text" 
                        name="posX" 
                        placeholder="Position X" 
                        value={posX ? parseFloat(posX).toFixed(4) : ''}
                        onChange={(e) => setPosX(e.target.value)} 
                        required 
                        readOnly 
                        className="position-input" 
                      />
                      <input 
                        type="text" 
                        name="posY" 
                        placeholder="Position Y" 
                        value={posY ? parseFloat(posY).toFixed(4) : ''}
                        onChange={(e) => setPosY(e.target.value)} 
                        required 
                        readOnly 
                        className="position-input" 
                      />
                      <input 
                        type="text" 
                        name="posZ" 
                        placeholder="Position Z" 
                        value={posZ ? parseFloat(posZ).toFixed(4) : ''}
                        onChange={(e) => setPosZ(e.target.value)} 
                        required 
                        readOnly 
                        className="position-input" 
                      />
                    </div>
                  </div>
                  
                  <div className="flex gap-4 justify-center">
                    <button 
                      type="button" 
                      onClick={(event) => handleSelectPositionClick(event)} 
                      className="button-type font-bold font-title text-xl px-4 py-2 flex items-center gap-2">
                      <ImLocation2 /> {t('position')}
                    </button>
                    <button 
                      type="submit" 
                      className="button-type font-bold font-title text-xl px-4 py-2 flex items-center gap-2">
                      {editInfospotMod ? <><FaPen /> {t('modify')}</> : t('add')}
                    </button>
                  </div>
                </div>

                {/* Right column - pas de justify-between ici */}
                <div className="flex flex-col h-full">
                  <input 
                    type="text" 
                    name="title" 
                    placeholder={t('titlePlaceholder')} 
                    required 
                    defaultValue={editInfospotMod ? infospotToEdit.title : ''} 
                    maxLength="45" 
                    className="p-2 rounded orange-border mb-2" 
                  />
                  <textarea 
                    name="text" 
                    placeholder={t('textPlaceholder')} 
                    required 
                    defaultValue={editInfospotMod ? infospotToEdit.text : ''} 
                    maxLength="300" 
                    className="p-2 rounded resize-none orange-border flex-grow" 
                  />

                  {/* Language and Visitor Type selectors */}
                  <div className="flex gap-2 mt-2">
                    <div className="flex-1">
                      <label className="text-junia-purple font-bold text-sm mb-1 block">{t('languageLabel')} :</label>
                      <select
                        value={selectedLanguage}
                        onChange={(e) => setSelectedLanguage(e.target.value)}
                        className="w-full p-2 rounded orange-border"
                        required
                      >
                        {languages.map((lang) => (
                          <option key={lang.id_language} value={lang.id_language}>
                            {lang.name_language}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="flex-1">
                      <label className="text-junia-purple font-bold text-sm mb-1 block">{t('visitorTypeLabel')} :</label>
                      <select
                        value={selectedVisitorType}
                        onChange={(e) => setSelectedVisitorType(e.target.value)}
                        className="w-full p-2 rounded orange-border"
                      >
                        <option value="">{t('allVisitors')}</option>
                        {visitorTypes.map((vt) => (
                          <option key={vt.id_visitor_type} value={vt.id_visitor_type}>
                            {vt.name_visitor_type}
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      {newLinkModalOpen && (
        <div className="fixed inset-0 flex justify-center items-center modal-background z-50 p-4">
          <div className="modal-infospot">
            {/* En-tête fixe */}
            <div className="modal-header">
              <div className="text-2xl font-bold text-junia-purple font-title text-center flex-grow">
                {editLinkMod ? t('editLink') : t('addNewLink')}
              </div>
              <button 
                className="modal-close-button" 
                onClick={closeModalLink}
              >
                &times;
              </button>
            </div>

            {/* Section Panorama avec hauteur fixe */}
            <div className="modal-panorama">
              <Panorama360
                infoPopups={modalInfoPopups}
                selectedPicture={modalSelectedPicture}
                links={modalLinks}
                onLinkClick={() => {}}
                onPositionSelect={handlePositionSelect}
                isLoading={isLoadingModal}
              />
            </div>

            {/* Conteneur de formulaire scrollable */}
            <div className="modal-form-container">
              <form onSubmit={editLinkMod ? handleEditLinkSubmit : handleNewLinkSubmit} 
                    className="grid grid-cols-2 gap-4">
                <input type="hidden" name="id_pictures" value={selectedImageId || ''} />
                
                {/* Left column */}
                <div className="flex flex-col gap-4 justify-between h-full">
                  <div>
                    <label className="font-title font-bold text-center w-full text-junia-purple mb-2 block">{t('destinationImageId')}</label>
                    <input
                      type="text"
                      name="id_pictures_destination"
                      placeholder={t('destinationIdPlaceholder')} 
                      required 
                      defaultValue={editLinkMod ? linkToEdit.id_pictures_destination : ''} 
                      className="p-2 rounded orange-border w-full" 
                    />
                  </div>
                  <div className="position-inputs-container flex-col">
                    {/* Labels en haut */}
                    <div className="flex w-full justify-between mb-1">
                      <label className="text-junia-purple font-bold text-center w-1/3">{t('coordinateX')} :</label>
                      <label className="text-junia-purple font-bold text-center w-1/3">{t('coordinateY')} :</label>
                      <label className="text-junia-purple font-bold text-center w-1/3">{t('coordinateZ')} :</label>
                    </div>
                    {/* Inputs en bas */}
                    <div className="flex w-full justify-between">
                      <input 
                        type="text" 
                        name="posX" 
                        placeholder="Position X" 
                        value={posX ? parseFloat(posX).toFixed(4) : ''}
                        onChange={(e) => setPosX(e.target.value)} 
                        required 
                        readOnly 
                        className="position-input" 
                      />
                      <input 
                        type="text" 
                        name="posY" 
                        placeholder="Position Y" 
                        value={posY ? parseFloat(posY).toFixed(4) : ''}
                        onChange={(e) => setPosY(e.target.value)} 
                        required 
                        readOnly 
                        className="position-input" 
                      />
                      <input 
                        type="text" 
                        name="posZ" 
                        placeholder="Position Z" 
                        value={posZ ? parseFloat(posZ).toFixed(4) : ''}
                        onChange={(e) => setPosZ(e.target.value)} 
                        required 
                        readOnly 
                        className="position-input" 
                      />
                    </div>
                  </div>
                  
                  <div className="flex gap-4 justify-center">
                    <button 
                      type="button" 
                      onClick={(event) => handleSelectPositionClick(event)} 
                      className="button-type font-bold font-title text-xl px-4 py-2 flex items-center gap-2">
                      <ImLocation2 /> {t('position')}
                    </button>
                    <button 
                      type="submit" 
                      className="button-type font-bold font-title text-xl px-4 py-2 flex items-center gap-2">
                      {editLinkMod ? <><FaPen /> {t('modify')}</> : t('add')}
                    </button>
                  </div>
                </div>

                {/* Right column - pas de justify-between ici */}
                <div className="flex flex-col h-full">
                  
                  <div className="flex-1 overflow-y-auto">
                    {pictures.map(picture => (
                      <div 
                        key={picture.id_pictures} 
                        className="mb-4 cursor-pointer bg-white p-2 rounded-lg hover:shadow-lg transition-all"
                        onClick={() => handleModalPictureClick(picture.imageUrl, picture.id_pictures)}
                      >
                        <p className="text-sm font-bold mb-2">ID : {picture.id_pictures}</p>
                        <img 
                          src={picture.imageUrl} 
                          alt={`Aperçu ${picture.id_pictures}`} 
                          className="w-full h-auto rounded-lg"
                        />
                      </div>
                    ))}
                  </div>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      <ModalAddEditImage isOpen={addImageModalOpen} toggle={
        () => setAddImageModalOpen(!addImageModalOpen)
      } id_rooms={id} imageToUpdate={imageToUpdate} reload={reloadAfterAddEditImage} />

      {/* Translation Modal */}
      {translationModalOpen && infospotForTranslation && (
        <div className="fixed inset-0 flex justify-center items-center modal-background z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl max-w-2xl w-full max-h-[90vh] overflow-hidden flex flex-col">
            {/* Header */}
            <div className="modal-header flex justify-between items-center p-4 border-b">
              <div className="text-2xl font-bold text-junia-purple font-title">
                {t('manageTranslations')} - InfoPopup #{infospotForTranslation.id_info_popup}
              </div>
              <button
                className="text-3xl text-gray-500 hover:text-gray-700"
                onClick={closeTranslationModal}
              >
                &times;
              </button>
            </div>

            {/* Content */}
            <div className="flex-1 overflow-y-auto p-4">
              {/* Existing translations */}
              <div className="mb-6">
                <h3 className="font-bold font-title text-junia-purple mb-3 flex items-center gap-2">
                  <FaGlobe /> {t('existingTranslations')} ({infospotTranslations.length})
                </h3>

                {infospotTranslations.length === 0 ? (
                  <div className="text-gray-500 italic p-4 bg-gray-50 rounded">
                    {t('noTranslationFound')}
                  </div>
                ) : (
                  <div className="space-y-3">
                    {infospotTranslations.map((trans) => (
                      <div key={trans.id_languages} className="bg-gray-50 p-3 rounded-lg border-l-4 border-junia-orange">
                        <div className="flex justify-between items-start">
                          <div className="flex-1">
                            <div className="flex items-center gap-2 mb-1">
                              <span className="bg-junia-purple text-white text-xs px-2 py-1 rounded">
                                {trans.name_language || getLanguageName(trans.id_languages)}
                              </span>
                              {trans.id_visitor_type && (
                                <span className="bg-junia-orange text-white text-xs px-2 py-1 rounded">
                                  {trans.name_visitor_type || getVisitorTypeName(trans.id_visitor_type)}
                                </span>
                              )}
                            </div>
                            <div className="font-bold text-junia-orange text-lg">{trans.title}</div>
                            <div className="text-gray-700 text-sm mt-1">{trans.text}</div>
                          </div>
                          <button
                            onClick={() => handleDeleteTranslation(trans.id_languages)}
                            className="ml-2 p-2 text-red-500 hover:bg-red-100 rounded"
                            title={t('deleteTranslationTooltip')}
                          >
                            <FaTrash />
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* Add new translation form */}
              <div className="border-t pt-4">
                <h3 className="font-bold font-title text-junia-purple mb-3 flex items-center gap-2">
                  <FaPlusCircle /> {t('addTranslationSection')}
                </h3>

                <form onSubmit={handleAddTranslation} className="space-y-4">
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="text-junia-purple font-bold text-sm mb-1 block">{t('languageLabel')} :</label>
                      <select
                        value={selectedLanguage}
                        onChange={(e) => setSelectedLanguage(e.target.value)}
                        className="w-full p-2 rounded orange-border"
                        required
                      >
                        {languages.map((lang) => (
                          <option
                            key={lang.id_language}
                            value={lang.id_language}
                            disabled={infospotTranslations.some(tr => tr.id_languages === lang.id_language)}
                          >
                            {lang.name_language}
                            {infospotTranslations.some(tr => tr.id_languages === lang.id_language) && ` (${t('alreadyTranslated')})`}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div>
                      <label className="text-junia-purple font-bold text-sm mb-1 block">{t('visitorTypeLabel')} :</label>
                      <select
                        value={selectedVisitorType}
                        onChange={(e) => setSelectedVisitorType(e.target.value)}
                        className="w-full p-2 rounded orange-border"
                      >
                        <option value="">{t('allVisitors')}</option>
                        {visitorTypes.map((vt) => (
                          <option key={vt.id_visitor_type} value={vt.id_visitor_type}>
                            {vt.name_visitor_type}
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>

                  <div>
                    <label className="text-junia-purple font-bold text-sm mb-1 block">{t('translationTitleLabel')} :</label>
                    <input
                      type="text"
                      value={newTranslationTitle}
                      onChange={(e) => setNewTranslationTitle(e.target.value)}
                      placeholder={t('translationTitlePlaceholder')}
                      className="w-full p-2 rounded orange-border"
                      maxLength="45"
                      required
                    />
                  </div>

                  <div>
                    <label className="text-junia-purple font-bold text-sm mb-1 block">{t('translationTextLabel')} :</label>
                    <textarea
                      value={newTranslationText}
                      onChange={(e) => setNewTranslationText(e.target.value)}
                      placeholder={t('translationTextPlaceholder')}
                      className="w-full p-2 rounded orange-border resize-none"
                      rows="4"
                      maxLength="300"
                      required
                    />
                  </div>

                  <div className="flex justify-end gap-2">
                    <button
                      type="button"
                      onClick={closeTranslationModal}
                      className="px-4 py-2 bg-gray-300 text-gray-700 rounded font-title font-bold"
                    >
                      {t('close')}
                    </button>
                    <button
                      type="submit"
                      className="button-type px-4 py-2 font-title font-bold flex items-center gap-2"
                    >
                      <FaPlusCircle /> {t('addTranslationBtn')}
                    </button>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </div>
      )}

      <ConfirmDialog open={showConfirm} onClose={() => setShowConfirm(false)} title={confirmTitle} message={confirmMessage} onConfirm={async () => {
        if (imageToDelete) {
          await confirmDeleteImage();
        } else if (infospotToDelete) {
          await confirmDeleteInfospot();
        } else if (linkToDelete) {
          await confirmDeleteLink();
        }
      }} />
    </div>
  );
};

export default AdminRoomDetails;